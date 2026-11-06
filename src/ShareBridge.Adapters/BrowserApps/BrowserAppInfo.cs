namespace ShareBridge.Adapters.BrowserApps;

public enum BrowserAppKind
{
    GenericPwa = 0,
    WhatsApp,
    Gmail
}

public sealed record BrowserAppInfo(
    string AppId,
    string DisplayName,
    BrowserAppKind Kind,
    string? ShortcutPath = null,
    string? TargetExe = null,
    string? Arguments = null,
    string? IconPath = null);
