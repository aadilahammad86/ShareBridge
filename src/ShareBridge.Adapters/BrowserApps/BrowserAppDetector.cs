using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ShareBridge.Adapters.BrowserApps;

public interface IBrowserAppDetector
{
    IReadOnlyList<BrowserAppInfo> DetectInstalledApps();
}

public sealed class BrowserAppDetector : IBrowserAppDetector
{
    public IReadOnlyList<BrowserAppInfo> DetectInstalledApps()
    {
        var apps = new List<BrowserAppInfo>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Scan Windows Registry for installed Chromium web apps
        try
        {
            using var uninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using var subKey = uninstallKey.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    var uninstallString = subKey.GetValue("UninstallString") as string;
                    var displayName = subKey.GetValue("DisplayName") as string;
                    var displayIcon = subKey.GetValue("DisplayIcon") as string;

                    if (!string.IsNullOrEmpty(uninstallString) && uninstallString.Contains("--uninstall-app-id="))
                    {
                        var appId = ExtractAppId(uninstallString);
                        if (!string.IsNullOrEmpty(appId) && seenIds.Add(appId))
                        {
                            var name = !string.IsNullOrWhiteSpace(displayName) ? displayName : "Web App";
                            var kind = ClassifyApp(name, appId);
                            var shortcut = FindShortcutForApp(name);

                            apps.Add(new BrowserAppInfo(
                                AppId: appId,
                                DisplayName: name,
                                Kind: kind,
                                ShortcutPath: shortcut,
                                IconPath: displayIcon
                            ));
                        }
                    }
                }
            }
        }
        catch
        {
            // Registry fallback
        }

        // 2. Scan Chrome Apps & Edge Apps Start Menu folders for shortcuts
        ScanShortcutFolder(Environment.SpecialFolder.ApplicationData, @"Microsoft\Windows\Start Menu\Programs\Chrome Apps", apps, seenIds);
        ScanShortcutFolder(Environment.SpecialFolder.ApplicationData, @"Microsoft\Windows\Start Menu\Programs\Edge Apps", apps, seenIds);

        return apps;
    }

    private static void ScanShortcutFolder(
        Environment.SpecialFolder root,
        string subDir,
        List<BrowserAppInfo> apps,
        HashSet<string> seenIds)
    {
        try
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(root), subDir);
            if (!Directory.Exists(baseDir)) return;

            foreach (var file in Directory.GetFiles(baseDir, "*.lnk"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var appId = fileName.Replace(" ", "").ToLowerInvariant();

                if (seenIds.Add(appId))
                {
                    var kind = ClassifyApp(fileName, appId);
                    apps.Add(new BrowserAppInfo(
                        AppId: appId,
                        DisplayName: fileName,
                        Kind: kind,
                        ShortcutPath: file
                    ));
                }
            }
        }
        catch
        {
            // Directory read fallback
        }
    }

    public static BrowserAppKind ClassifyApp(string displayName, string appId)
    {
        var lowerName = displayName.ToLowerInvariant();

        if (lowerName.Contains("whatsapp") || appId.Equals("hnpfjngllnobngcgfapefoaidbinmjnm", StringComparison.OrdinalIgnoreCase))
        {
            return BrowserAppKind.WhatsApp;
        }

        if (lowerName.Contains("gmail") || lowerName.Contains("google mail"))
        {
            return BrowserAppKind.Gmail;
        }

        return BrowserAppKind.GenericPwa;
    }

    public static string? ExtractAppId(string uninstallString)
    {
        const string marker = "--uninstall-app-id=";
        int idx = uninstallString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var remainder = uninstallString[(idx + marker.Length)..].Trim();
        int space = remainder.IndexOf(' ');
        var id = space > 0 ? remainder[..space] : remainder;
        return id.Trim('\"');
    }

    private static string? FindShortcutForApp(string displayName)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var chromeLnk = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs\Chrome Apps", $"{displayName}.lnk");
        if (File.Exists(chromeLnk)) return chromeLnk;

        var edgeLnk = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs\Edge Apps", $"{displayName}.lnk");
        if (File.Exists(edgeLnk)) return edgeLnk;

        return null;
    }
}
