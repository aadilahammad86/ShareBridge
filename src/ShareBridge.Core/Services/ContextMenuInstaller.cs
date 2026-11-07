using System;
using System.IO;
using Microsoft.Win32;

namespace ShareBridge.Core.Services;

public class ContextMenuInstaller
{
    private const string BaseKey = @"Software\Classes\*\shell\ShareBridge";

    public static void EnsureInstalled()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                Install(exe, File.Exists(icon) ? icon : exe);
            }
        }
        catch
        {
            // Best-effort registration
        }
    }

    public static void Install(string executablePath, string? iconPath = null)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new ArgumentException("Executable path is required.", nameof(executablePath));

        iconPath ??= executablePath;

        // Root key: HKCU\Software\Classes\*\shell\ShareBridge
        using (var root = Registry.CurrentUser.CreateSubKey(BaseKey))
        {
            root.SetValue("MUIVerb", "Share Bridge");
            root.SetValue("Icon", iconPath);
            root.SetValue("SubCommands", "");
        }

        // Subkey shell
        var shellPath = $@"{BaseKey}\shell";

        // Find installed app icons dynamically
        var telegramIcon = FindTelegramIcon() ?? iconPath;
        var clipboardIcon = "shell32.dll,134";
        var whatsAppIcon = FindWhatsAppIcon() ?? "shell32.dll,135";
        var gmailIcon = FindChromeIcon() ?? "shell32.dll,18";

        // 1. Telegram
        AddSubCommand(shellPath, "01_Telegram", "Send via Telegram", telegramIcon, $"\"{executablePath}\" --target telegram \"%1\"");

        // 2. Clipboard
        AddSubCommand(shellPath, "02_Clipboard", "Copy to Clipboard", clipboardIcon, $"\"{executablePath}\" --target clipboard \"%1\"");

        // 3. WhatsApp Web
        AddSubCommand(shellPath, "03_WhatsApp", "Send via WhatsApp", whatsAppIcon, $"\"{executablePath}\" --target whatsapp \"%1\"");

        // 4. Gmail
        AddSubCommand(shellPath, "04_Gmail", "Send via Gmail", gmailIcon, $"\"{executablePath}\" --target gmail \"%1\"");

        // 5. Open Picker Window
        AddSubCommand(shellPath, "05_Picker", "Open Share Bridge...", iconPath, $"\"{executablePath}\" \"%1\"");
    }

    private static string? FindTelegramIcon()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\tg\shell\open\command");
            if (key?.GetValue(null) is string cmd && !string.IsNullOrWhiteSpace(cmd))
            {
                var path = cmd.Trim().Trim('"');
                int exeIdx = path.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (exeIdx > 0)
                {
                    var exe = path.Substring(0, exeIdx + 4);
                    if (File.Exists(exe)) return $"{exe},0";
                }
            }
        }
        catch { }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var defaultPath = Path.Combine(appData, "Telegram Desktop", "Telegram.exe");
        return File.Exists(defaultPath) ? $"{defaultPath},0" : null;
    }

    private static string? FindWhatsAppIcon()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromeWebApps = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Web Applications");
            if (Directory.Exists(chromeWebApps))
            {
                var icoFiles = Directory.GetFiles(chromeWebApps, "*WhatsApp*.ico", SearchOption.AllDirectories);
                if (icoFiles.Length > 0) return $"{icoFiles[0]},0";
            }
        }
        catch { }
        return null;
    }

    private static string? FindChromeIcon()
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var chrome = Path.Combine(pf, @"Google\Chrome\Application\chrome.exe");
        if (File.Exists(chrome)) return $"{chrome},0";

        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var chromex86 = Path.Combine(pfx86, @"Google\Chrome\Application\chrome.exe");
        if (File.Exists(chromex86)) return $"{chromex86},0";

        return null;
    }

    public static void Uninstall()
    {
        Registry.CurrentUser.DeleteSubKeyTree(BaseKey, throwOnMissingSubKey: false);
    }

    public static bool IsInstalled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(BaseKey);
        return key != null;
    }

    private static void AddSubCommand(string parentPath, string keyName, string title, string icon, string command)
    {
        using var subKey = Registry.CurrentUser.CreateSubKey($@"{parentPath}\{keyName}");
        subKey.SetValue("MUIVerb", title);
        if (!string.IsNullOrEmpty(icon))
        {
            subKey.SetValue("Icon", icon);
        }

        using var cmdKey = subKey.CreateSubKey("command");
        cmdKey.SetValue("", command);
    }
}
