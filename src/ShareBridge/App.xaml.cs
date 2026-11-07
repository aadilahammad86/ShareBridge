using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ShareBridge.Adapters.BrowserApps;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Adapters.Telegram;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;
using ShareBridge.Core.Services;
using ShareBridge.Views;

namespace ShareBridge;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

        if (activatedArgs.Kind == ExtendedActivationKind.ShareTarget)
        {
            // Share Target activation from Windows Share panel
            if (activatedArgs.Data is ShareTargetActivatedEventArgs shareArgs)
            {
                _window = new SharePickerWindow(shareArgs.ShareOperation);
                _window.Activate();
                return;
            }
        }

        // Check command-line arguments (from Context Menu or CLI)
        var cmdArgs = Environment.GetCommandLineArgs();
        if (cmdArgs.Length > 1)
        {
            var parsed = ParseCommandLine(cmdArgs);
            if (parsed.Files.Count > 0)
            {
                if (!string.IsNullOrEmpty(parsed.Target))
                {
                    // Direct dispatch without opening picker UI
                    await ExecuteDirectTargetAsync(parsed.Target, parsed.Files);
                    Current.Exit();
                    return;
                }
                else
                {
                    // Open picker UI with the provided file(s) preloaded
                    _window = new SharePickerWindow(directFiles: parsed.Files.ToArray());
                    _window.Activate();
                    return;
                }
            }
        }

        // Standard user launch from Start Menu or Taskbar
        ContextMenuInstaller.EnsureInstalled();
        _window = new MainWindow();
        _window.Activate();
    }

    private static (string? Target, List<string> Files) ParseCommandLine(string[] args)
    {
        string? target = null;
        var files = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                target = args[++i];
            }
            else if (arg.StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
            {
                target = arg.Substring("--target=".Length);
            }
            else if (!arg.StartsWith("-") && File.Exists(arg))
            {
                files.Add(Path.GetFullPath(arg));
            }
        }

        return (target, files);
    }

    private static async Task ExecuteDirectTargetAsync(string target, List<string> files)
    {
        var registry = new AdapterRegistry();
        registry.RegisterAdapter(new TelegramAdapter());
        registry.RegisterAdapter(new ClipboardAdapter());

        var detector = new BrowserAppDetector();
        foreach (var app in detector.DetectInstalledApps())
        {
            registry.RegisterAdapter(new BrowserAppAdapter(app));
        }

        var payload = new SharePayload
        {
            Type = PayloadType.Files,
            FilePaths = files
        };

        var adapter = registry.GetAllAdapters()
            .FirstOrDefault(a => a.Id.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                                 a.DisplayName.Contains(target, StringComparison.OrdinalIgnoreCase));

        if (adapter != null && adapter.CanHandle(payload))
        {
            await adapter.SendAsync(payload);
        }
    }
}
