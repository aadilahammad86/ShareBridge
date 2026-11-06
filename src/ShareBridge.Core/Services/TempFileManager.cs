using System;
using System.IO;

namespace ShareBridge.Core.Services;

public interface ITempFileManager
{
    string RootTempPath { get; }
    string CreateSessionDirectory(Guid? sessionId = null);
    void CleanSession(string sessionDirectory);
    int CleanStaleSessions(TimeSpan maxAge);
}

public sealed class TempFileManager : ITempFileManager
{
    private readonly string _rootTempPath;

    public TempFileManager(string? customRootPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customRootPath))
        {
            _rootTempPath = customRootPath;
        }
        else
        {
            _rootTempPath = DetermineDefaultRootPath();
        }

        Directory.CreateDirectory(_rootTempPath);
    }

    public string RootTempPath => _rootTempPath;

    public string CreateSessionDirectory(Guid? sessionId = null)
    {
        var id = sessionId ?? Guid.NewGuid();
        var sessionPath = Path.Combine(_rootTempPath, id.ToString("N"));
        Directory.CreateDirectory(sessionPath);
        return sessionPath;
    }

    public void CleanSession(string sessionDirectory)
    {
        if (Directory.Exists(sessionDirectory))
        {
            try
            {
                Directory.Delete(sessionDirectory, recursive: true);
            }
            catch
            {
                // Ignored - will be cleaned up by CleanStaleSessions
            }
        }
    }

    public int CleanStaleSessions(TimeSpan maxAge)
    {
        if (!Directory.Exists(_rootTempPath)) return 0;

        int cleaned = 0;
        var now = DateTime.UtcNow;
        var directories = Directory.GetDirectories(_rootTempPath);

        foreach (var dir in directories)
        {
            try
            {
                var dirInfo = new DirectoryInfo(dir);
                if (now - dirInfo.LastWriteTimeUtc > maxAge)
                {
                    dirInfo.Delete(recursive: true);
                    cleaned++;
                }
            }
            catch
            {
                // Skip locked directory
            }
        }

        return cleaned;
    }

    private static string DetermineDefaultRootPath()
    {
        try
        {
            // If running inside MSIX package
            var appDataTemp = Windows.Storage.ApplicationData.Current?.TemporaryFolder?.Path;
            if (!string.IsNullOrWhiteSpace(appDataTemp))
            {
                return Path.Combine(appDataTemp, "ShareBridge");
            }
        }
        catch
        {
            // Unpackaged / Unit test fallback
        }

        return Path.Combine(Path.GetTempPath(), "ShareBridge", "Temp");
    }
}
