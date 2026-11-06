using System;
using System.Collections.Generic;
using System.IO;

namespace ShareBridge.Core.Models;

[Flags]
public enum PayloadType
{
    None = 0,
    Text = 1,
    WebLink = 2,
    Files = 4,
    Bitmap = 8
}

public sealed class SharePayload : IDisposable
{
    public PayloadType Type { get; set; } = PayloadType.None;
    public IReadOnlyList<string> FilePaths { get; set; } = Array.Empty<string>();
    public string? Text { get; set; }
    public Uri? WebLink { get; set; }
    public string? SuggestedFileName { get; set; }
    public string? SessionDirectory { get; set; }
    public bool AutoCleanupOnDispose { get; set; } = true;

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (AutoCleanupOnDispose && !string.IsNullOrWhiteSpace(SessionDirectory) && Directory.Exists(SessionDirectory))
        {
            try
            {
                Directory.Delete(SessionDirectory, recursive: true);
            }
            catch
            {
                // Ignored - stale sessions will be cleaned on next activation
            }
        }
    }
}
