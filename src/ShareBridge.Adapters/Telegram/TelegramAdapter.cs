using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;

namespace ShareBridge.Adapters.Telegram;

public sealed class TelegramAdapter : ITargetAdapter
{
    public const string AdapterId = "telegram-desktop";

    public string Id => AdapterId;
    public string DisplayName => "Telegram";
    public string Description => "Share to Telegram Desktop";
    public string? IconGlyph => "\uE715"; // Send / Mail icon in Segoe Fluent Icons
    public string? IconPath => null;

    private readonly Func<string?>? _customPathProvider;

    public TelegramAdapter(Func<string?>? customPathProvider = null)
    {
        _customPathProvider = customPathProvider;
    }

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(FindExecutablePath());
    }

    public bool CanHandle(SharePayload payload)
    {
        if (payload == null) return false;

        // Telegram can handle files, text, and web links
        return (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0) ||
               (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null) ||
               (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text));
    }

    public Task<AdapterResult> SendAsync(SharePayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!CanHandle(payload))
        {
            return Task.FromResult(AdapterResult.Fail(AdapterErrorKind.UnsupportedContent, "Telegram cannot handle this content type."));
        }

        var exePath = FindExecutablePath();
        if (string.IsNullOrEmpty(exePath))
        {
            return Task.FromResult(AdapterResult.Fail(AdapterErrorKind.AppNotFound, "Telegram Desktop was not found on this system."));
        }

        try
        {
            var startInfo = BuildLaunchInfo(payload, exePath);
            using var process = Process.Start(startInfo);
            if (process == null && !startInfo.UseShellExecute)
            {
                return Task.FromResult(AdapterResult.Fail(AdapterErrorKind.LaunchFailed, "Failed to start Telegram process."));
            }

            return Task.FromResult(AdapterResult.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(AdapterResult.Fail(AdapterErrorKind.LaunchFailed, $"Error launching Telegram: {ex.Message}"));
        }
    }

    public ProcessStartInfo BuildLaunchInfo(SharePayload payload, string? executablePath = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var exe = executablePath ?? FindExecutablePath() ?? "Telegram.exe";

        // Case 1: Files present -> use -sendpath
        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            var argsBuilder = new StringBuilder("-sendpath");
            foreach (var file in payload.FilePaths)
            {
                argsBuilder.Append(" \"").Append(file).Append('\"');
            }

            return new ProcessStartInfo
            {
                FileName = exe,
                Arguments = argsBuilder.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        // Case 2: WebLink present
        if (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null)
        {
            var urlEncoded = Uri.EscapeDataString(payload.WebLink.ToString());
            var textParam = !string.IsNullOrWhiteSpace(payload.Text)
                ? $"&text={Uri.EscapeDataString(payload.Text)}"
                : string.Empty;

            var uriString = $"tg://msg_url?url={urlEncoded}{textParam}";

            return new ProcessStartInfo
            {
                FileName = uriString,
                UseShellExecute = true
            };
        }

        // Case 3: Plain text
        if (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text))
        {
            var textEncoded = Uri.EscapeDataString(payload.Text);
            var uriString = $"tg://msg?text={textEncoded}";

            return new ProcessStartInfo
            {
                FileName = uriString,
                UseShellExecute = true
            };
        }

        throw new InvalidOperationException("Payload contains no supported Telegram content.");
    }

    public string? FindExecutablePath()
    {
        // 1. Custom path override
        var custom = _customPathProvider?.Invoke();
        if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
        {
            return custom;
        }

        // 2. Registry protocol handler
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\tg\shell\open\command");
            if (key?.GetValue(null) is string cmd && !string.IsNullOrWhiteSpace(cmd))
            {
                var extracted = ExtractExecutableFromCommand(cmd);
                if (extracted != null && File.Exists(extracted))
                {
                    return extracted;
                }
            }
        }
        catch
        {
            // Registry read fallback
        }

        // 3. Default AppData installation path
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var defaultPath = Path.Combine(appData, "Telegram Desktop", "Telegram.exe");
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        // 4. LocalAppData installation path
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localPath = Path.Combine(localAppData, "Telegram Desktop", "Telegram.exe");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        // 5. Program Files path
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfPath = Path.Combine(progFiles, "Telegram Desktop", "Telegram.exe");
        if (File.Exists(pfPath))
        {
            return pfPath;
        }

        return null;
    }

    private static string? ExtractExecutableFromCommand(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.StartsWith('\"'))
        {
            int secondQuote = trimmed.IndexOf('\"', 1);
            if (secondQuote > 1)
            {
                return trimmed[1..secondQuote];
            }
        }
        else
        {
            int space = trimmed.IndexOf(' ');
            return space > 0 ? trimmed[..space] : trimmed;
        }

        return null;
    }
}
