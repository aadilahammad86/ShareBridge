using System;
using System.IO;
using Microsoft.Win32;

namespace ShareBridge.Core.Services;

public class ContextMenuInstaller
{
    private const string BaseKey = @"Software\Classes\*\shell\ShareBridge";

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

        // 1. Telegram
        AddSubCommand(shellPath, "01_Telegram", "Send via Telegram", $"{iconPath}", $"\"{executablePath}\" --target telegram \"%1\"");

        // 2. Clipboard
        AddSubCommand(shellPath, "02_Clipboard", "Copy to Clipboard", $"{iconPath}", $"\"{executablePath}\" --target clipboard \"%1\"");

        // 3. WhatsApp Web
        AddSubCommand(shellPath, "03_WhatsApp", "Send via WhatsApp", $"{iconPath}", $"\"{executablePath}\" --target whatsapp \"%1\"");

        // 4. Gmail
        AddSubCommand(shellPath, "04_Gmail", "Send via Gmail", $"{iconPath}", $"\"{executablePath}\" --target gmail \"%1\"");

        // 5. Open Picker Window
        AddSubCommand(shellPath, "05_Picker", "Open Share Bridge...", $"{iconPath}", $"\"{executablePath}\" \"%1\"");
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
