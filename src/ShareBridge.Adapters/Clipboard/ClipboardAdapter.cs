using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;

namespace ShareBridge.Adapters.Clipboard;

public sealed class ClipboardAdapter : ITargetAdapter
{
    public const string AdapterId = "clipboard";

    public string Id => AdapterId;
    public string DisplayName => "Copy to Clipboard";
    public string Description => "Copy shared files, text, or links to the clipboard";
    public string? IconGlyph => "\uE8C8"; // Copy icon in Segoe Fluent Icons
    public string? IconPath => null;

    public bool IsAvailable() => true;

    public bool CanHandle(SharePayload payload)
    {
        if (payload == null) return false;

        return (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0) ||
               (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null) ||
               (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text));
    }

    public async Task<AdapterResult> SendAsync(SharePayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!CanHandle(payload))
        {
            return AdapterResult.Fail(AdapterErrorKind.UnsupportedContent, "Nothing to copy to clipboard.");
        }

        try
        {
            var dataPackage = new DataPackage();

            // 1. Files
            if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
            {
                var storageItems = new List<IStorageItem>();
                foreach (var file in payload.FilePaths)
                {
                    if (File.Exists(file))
                    {
                        var storageFile = await StorageFile.GetFileFromPathAsync(file);
                        storageItems.Add(storageFile);
                    }
                }

                if (storageItems.Count > 0)
                {
                    dataPackage.SetStorageItems(storageItems);
                }

                // Also populate text format with file paths so pasting into plain text editors works
                var formatted = FormatTextRepresentation(payload);
                dataPackage.SetText(formatted);
            }
            // 2. WebLink
            else if (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null)
            {
                dataPackage.SetWebLink(payload.WebLink);
                var formatted = FormatTextRepresentation(payload);
                dataPackage.SetText(formatted);
            }
            // 3. Text
            else if (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text))
            {
                dataPackage.SetText(payload.Text);
            }

            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            Windows.ApplicationModel.DataTransfer.Clipboard.Flush();

            return AdapterResult.Ok();
        }
        catch (Exception ex)
        {
            return AdapterResult.Fail(AdapterErrorKind.LaunchFailed, $"Failed to copy to clipboard: {ex.Message}");
        }
    }

    public string FormatTextRepresentation(SharePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var sb = new StringBuilder();

        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            for (int i = 0; i < payload.FilePaths.Count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(payload.FilePaths[i]);
            }
            return sb.ToString();
        }

        if (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null)
        {
            if (!string.IsNullOrWhiteSpace(payload.Text))
            {
                sb.AppendLine(payload.Text);
            }
            sb.Append(payload.WebLink);
            return sb.ToString();
        }

        if (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text))
        {
            return payload.Text;
        }

        return string.Empty;
    }
}
