using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;

namespace ShareBridge.Adapters.BrowserApps;

public sealed class BrowserAppAdapter : ITargetAdapter
{
    private readonly BrowserAppInfo _appInfo;
    private readonly ClipboardAdapter _clipboardAdapter;

    public BrowserAppAdapter(BrowserAppInfo appInfo)
    {
        _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
        _clipboardAdapter = new ClipboardAdapter();
    }

    public string Id => $"browser-app-{_appInfo.AppId}";
    public string DisplayName => _appInfo.DisplayName;
    public string Description => $"Share to {_appInfo.DisplayName}";
    public string? IconGlyph => _appInfo.Kind switch
    {
        BrowserAppKind.WhatsApp => "\uE134", // Chat
        BrowserAppKind.Gmail => "\uE715",    // Mail
        _ => "\uE774"                       // Globe / Web
    };
    public string? IconPath => _appInfo.IconPath;
    public BrowserAppInfo AppInfo => _appInfo;

    public bool IsAvailable()
    {
        // Available if either shortcut exists or app id is known
        if (!string.IsNullOrEmpty(_appInfo.ShortcutPath) && File.Exists(_appInfo.ShortcutPath))
        {
            return true;
        }

        return !string.IsNullOrEmpty(_appInfo.AppId);
    }

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
            return AdapterResult.Fail(AdapterErrorKind.UnsupportedContent, "Unsupported content for this web application.");
        }

        try
        {
            // Case 1: WhatsApp
            if (_appInfo.Kind == BrowserAppKind.WhatsApp)
            {
                return await HandleWhatsAppAsync(payload, cancellationToken);
            }

            // Case 2: Gmail
            if (_appInfo.Kind == BrowserAppKind.Gmail)
            {
                return await HandleGmailAsync(payload, cancellationToken);
            }

            // Case 3: Generic PWA
            return await HandleGenericPwaAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            return AdapterResult.Fail(AdapterErrorKind.LaunchFailed, $"Error launching {_appInfo.DisplayName}: {ex.Message}");
        }
    }

    private async Task<AdapterResult> HandleWhatsAppAsync(SharePayload payload, CancellationToken cancellationToken)
    {
        // If sharing files: assisted clipboard flow + focus WhatsApp window
        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            await _clipboardAdapter.SendAsync(payload, cancellationToken);

            // Launch shortcut to bring WhatsApp to front
            LaunchAppShortcutOrUrl("https://web.whatsapp.com");

            return AdapterResult.Ok();
        }

        // Sharing text or link
        var textToShare = BuildTextOrLinkContent(payload);
        var waUrl = $"https://wa.me/?text={Uri.EscapeDataString(textToShare)}";

        LaunchUrl(waUrl);
        return AdapterResult.Ok();
    }

    private async Task<AdapterResult> HandleGmailAsync(SharePayload payload, CancellationToken cancellationToken)
    {
        // If sharing files: copy to clipboard and open compose window
        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            await _clipboardAdapter.SendAsync(payload, cancellationToken);

            var composeUrl = "https://mail.google.com/mail/?view=cm&fs=1";
            LaunchUrl(composeUrl);

            return AdapterResult.Ok();
        }

        // Sharing text or link
        var body = BuildTextOrLinkContent(payload);
        var subject = payload.WebLink != null ? "Shared Link" : "Shared Content";
        var gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&su={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

        LaunchUrl(gmailUrl);
        return AdapterResult.Ok();
    }

    private async Task<AdapterResult> HandleGenericPwaAsync(SharePayload payload, CancellationToken cancellationToken)
    {
        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            await _clipboardAdapter.SendAsync(payload, cancellationToken);
        }

        if (!string.IsNullOrEmpty(_appInfo.ShortcutPath) && File.Exists(_appInfo.ShortcutPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _appInfo.ShortcutPath,
                UseShellExecute = true
            });
            return AdapterResult.Ok();
        }

        return AdapterResult.Fail(AdapterErrorKind.LaunchFailed, "Shortcut for web application not found.");
    }

    private string BuildTextOrLinkContent(SharePayload payload)
    {
        if (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null)
        {
            return !string.IsNullOrWhiteSpace(payload.Text)
                ? $"{payload.Text}\n{payload.WebLink}"
                : payload.WebLink.ToString();
        }

        return payload.Text ?? string.Empty;
    }

    private void LaunchAppShortcutOrUrl(string fallbackUrl)
    {
        if (!string.IsNullOrEmpty(_appInfo.ShortcutPath) && File.Exists(_appInfo.ShortcutPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _appInfo.ShortcutPath,
                UseShellExecute = true
            });
        }
        else
        {
            LaunchUrl(fallbackUrl);
        }
    }

    private static void LaunchUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
