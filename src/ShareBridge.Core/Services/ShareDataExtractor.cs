using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using ShareBridge.Core.Models;

namespace ShareBridge.Core.Services;

public interface IShareDataExtractor
{
    Task<SharePayload> ExtractAsync(DataPackageView dataPackageView);
}

public sealed class ShareDataExtractor : IShareDataExtractor
{
    private readonly ITempFileManager _tempFileManager;

    public ShareDataExtractor(ITempFileManager tempFileManager)
    {
        _tempFileManager = tempFileManager ?? throw new ArgumentNullException(nameof(tempFileManager));
    }

    public async Task<SharePayload> ExtractAsync(DataPackageView dataPackageView)
    {
        ArgumentNullException.ThrowIfNull(dataPackageView);

        var payload = new SharePayload();
        string? sessionDir = null;

        // 1. Files / StorageItems
        if (dataPackageView.Contains(StandardDataFormats.StorageItems))
        {
            try
            {
                var items = await dataPackageView.GetStorageItemsAsync();
                if (items != null && items.Count > 0)
                {
                    sessionDir = _tempFileManager.CreateSessionDirectory();
                    payload.SessionDirectory = sessionDir;
                    var filePaths = new List<string>();

                    foreach (var item in items)
                    {
                        if (item is StorageFile storageFile)
                        {
                            var destPath = Path.Combine(sessionDir, storageFile.Name);
                            // Ensure unique file name if collision
                            destPath = EnsureUniqueFilePath(destPath);

                            using (var sourceStream = await storageFile.OpenStreamForReadAsync())
                            using (var destStream = File.Create(destPath))
                            {
                                await sourceStream.CopyToAsync(destStream);
                            }

                            filePaths.Add(destPath);
                        }
                        else if (!string.IsNullOrEmpty(item.Path) && File.Exists(item.Path))
                        {
                            // If direct path is accessible
                            var destPath = Path.Combine(sessionDir, Path.GetFileName(item.Path));
                            destPath = EnsureUniqueFilePath(destPath);
                            File.Copy(item.Path, destPath, overwrite: true);
                            filePaths.Add(destPath);
                        }
                    }

                    if (filePaths.Count > 0)
                    {
                        payload.FilePaths = filePaths;
                        payload.Type |= PayloadType.Files;
                    }
                }
            }
            catch
            {
                // Fallback to text/links if storage item extraction fails
            }
        }

        // 2. WebLink
        if (dataPackageView.Contains(StandardDataFormats.WebLink))
        {
            try
            {
                var uri = await dataPackageView.GetWebLinkAsync();
                if (uri != null)
                {
                    payload.WebLink = uri;
                    payload.Type |= PayloadType.WebLink;
                }
            }
            catch
            {
                // Ignored
            }
        }

        // 3. Text
        if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            try
            {
                var text = await dataPackageView.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    payload.Text = text;
                    payload.Type |= PayloadType.Text;

                    // If text is a valid URI and WebLink wasn't explicitly populated
                    if (payload.WebLink == null && Uri.TryCreate(text.Trim(), UriKind.Absolute, out var parsedUri) &&
                        (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
                    {
                        payload.WebLink = parsedUri;
                        payload.Type |= PayloadType.WebLink;
                    }
                }
            }
            catch
            {
                // Ignored
            }
        }

        // 4. Bitmap (in-memory image without storage item)
        if (dataPackageView.Contains(StandardDataFormats.Bitmap) && !payload.Type.HasFlag(PayloadType.Files))
        {
            try
            {
                var bitmapRef = await dataPackageView.GetBitmapAsync();
                if (bitmapRef != null)
                {
                    sessionDir ??= _tempFileManager.CreateSessionDirectory();
                    payload.SessionDirectory = sessionDir;

                    var bitmapPath = Path.Combine(sessionDir, "shared_image.png");
                    using (var stream = await bitmapRef.OpenReadAsync())
                    using (var netStream = stream.AsStreamForRead())
                    using (var fileStream = File.Create(bitmapPath))
                    {
                        await netStream.CopyToAsync(fileStream);
                    }

                    if (File.Exists(bitmapPath) && new FileInfo(bitmapPath).Length > 0)
                    {
                        payload.FilePaths = new[] { bitmapPath };
                        payload.Type |= PayloadType.Bitmap | PayloadType.Files;
                    }
                }
            }
            catch
            {
                // Ignored
            }
        }

        return payload;
    }

    private static string EnsureUniqueFilePath(string path)
    {
        if (!File.Exists(path)) return path;

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        int counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){ext}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }
}
