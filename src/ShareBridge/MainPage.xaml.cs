using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShareBridge.Adapters.BrowserApps;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Adapters.Telegram;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Services;
using ShareBridge.Views;

namespace ShareBridge;

public sealed partial class MainPage : Page
{
    private readonly TempFileManager _tempManager;
    private readonly AdapterRegistry _registry;

    public MainPage()
    {
        InitializeComponent();
        _tempManager = new TempFileManager();
        _registry = new AdapterRegistry();
        InstalledVersionText.Text = $"Installed Version: v{GetRunningVersion()}";

        Loaded += (s, e) =>
        {
            RefreshAdapters();
            InstalledVersionText.Text = $"Installed Version: v{GetRunningVersion()}";
        };
    }

    private static string GetRunningVersion()
    {
        try
        {
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        catch
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.1";
        }
    }

    private void RefreshAdapters()
    {
        AdaptersListPanel.Children.Clear();

        // 1. Telegram Adapter
        var telegram = new TelegramAdapter();
        AddAdapterStatusRow(telegram);

        // 2. Clipboard Adapter
        var clipboard = new ClipboardAdapter();
        AddAdapterStatusRow(clipboard);

        // 3. Browser App Adapters
        var detector = new BrowserAppDetector();
        var browserApps = detector.DetectInstalledApps();

        foreach (var app in browserApps)
        {
            var pwaAdapter = new BrowserAppAdapter(app);
            AddAdapterStatusRow(pwaAdapter);
        }

        StatusMessage.Text = $"Detected {browserApps.Count + 2} active adapters. Ready for Windows 11 Share panel.";
    }

    private void AddAdapterStatusRow(ITargetAdapter adapter)
    {
        var row = new Border
        {
            Background = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10)
        };

        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon
        var icon = new FontIcon
        {
            Glyph = adapter.IconGlyph ?? "\uE71D",
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        // Text
        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        textStack.Children.Add(new TextBlock
        {
            Text = adapter.DisplayName,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 14
        });
        textStack.Children.Add(new TextBlock
        {
            Text = adapter.Description,
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        // Status badge
        var isAvailable = adapter.IsAvailable();
        var badge = new Border
        {
            Background = isAvailable
                ? Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"] as Brush
                : Application.Current.Resources["SystemFillColorCautionBackgroundBrush"] as Brush,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock
        {
            Text = isAvailable ? "Available" : "Not Found",
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.Medium,
            Foreground = isAvailable
                ? Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush
                : Application.Current.Resources["SystemFillColorCautionBrush"] as Brush
        };
        Grid.SetColumn(badge, 2);
        grid.Children.Add(badge);

        row.Child = grid;
        AdaptersListPanel.Children.Add(row);
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshAdapters();
    }

    private void OnTestPickerClick(object sender, RoutedEventArgs e)
    {
        var testPicker = new SharePickerWindow();
        testPicker.Activate();
    }

    private void OnCleanTempClick(object sender, RoutedEventArgs e)
    {
        var cleaned = _tempManager.CleanStaleSessions(TimeSpan.Zero);
        StatusMessage.Text = $"Cleaned {cleaned} temporary session directories.";
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = "Checking GitHub Releases...";
        UpdateStatusText.Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush;
        UpdateDownloadLink.Visibility = Visibility.Collapsed;
        UpdateProgressPanel.Visibility = Visibility.Collapsed;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ShareBridge-Updater");

            var url = "https://api.github.com/repos/aadilahammad86/ShareBridge/releases/latest";
            HttpResponseMessage? response = null;

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    response = await client.GetAsync(url);
                    break;
                }
                catch (HttpRequestException) when (attempt == 1)
                {
                    UpdateStatusText.Text = "Connection hiccup, retrying...";
                    await Task.Delay(1200);
                }
                catch (TaskCanceledException) when (attempt == 1)
                {
                    UpdateStatusText.Text = "Connection timed out, retrying...";
                    await Task.Delay(1200);
                }
            }

            if (response == null)
            {
                UpdateStatusText.Text = "• Offline: Unable to reach GitHub. Please check your internet connection.";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var curVer = GetRunningVersion();
                UpdateStatusText.Text = $"• Up to date (v{curVer} is the latest release)";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                UpdateStatusText.Text = "• GitHub request rate limit reached. Please try again in a few minutes.";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                UpdateStatusText.Text = $"• GitHub returned HTTP {(int)response.StatusCode}. Please try again later.";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                return;
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tagName = root.GetProperty("tag_name").GetString() ?? "";

            var cleanTag = tagName.TrimStart('v', 'V');
            var currentVersionStr = GetRunningVersion();

            Version.TryParse(cleanTag, out var latestVer);
            Version.TryParse(currentVersionStr, out var currentVer);

            bool isNewer = (latestVer != null && currentVer != null)
                ? latestVer > currentVer
                : !string.Equals(cleanTag, currentVersionStr, StringComparison.OrdinalIgnoreCase);

            if (isNewer)
            {
                UpdateStatusText.Text = $"• New version {tagName} found! Starting download...";
                UpdateStatusText.Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush;

                // Find installer asset (.exe)
                string? downloadUrl = null;
                string assetName = $"ShareBridge-Setup-{tagName}.exe";

                if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsElem.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            assetName = name;
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    UpdateStatusText.Text = $"• {tagName} available, but no .exe asset was found.";
                    UpdateDownloadLink.Visibility = Visibility.Visible;
                    return;
                }

                // Show progress bar
                UpdateProgressPanel.Visibility = Visibility.Visible;
                UpdateProgressBar.IsIndeterminate = false;
                UpdateProgressBar.Value = 0;
                UpdateProgressDetails.Text = "Connecting to GitHub...";

                var tempDir = Path.Combine(Path.GetTempPath(), "ShareBridgeUpdates");
                Directory.CreateDirectory(tempDir);
                var tempSetupPath = Path.Combine(tempDir, assetName);

                // Stream download with progress
                using (var downloadResponse = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    downloadResponse.EnsureSuccessStatusCode();
                    var totalBytes = downloadResponse.Content.Headers.ContentLength ?? -1L;
                    using var stream = await downloadResponse.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(tempSetupPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes > 0)
                        {
                            double pct = (double)totalRead / totalBytes * 100.0;
                            UpdateProgressBar.Value = pct;
                            UpdateProgressDetails.Text = $"Downloading {tagName}: {pct:0}% ({totalRead / 1048576.0:0.0} MB / {totalBytes / 1048576.0:0.0} MB)";
                        }
                        else
                        {
                            UpdateProgressDetails.Text = $"Downloading {tagName}: {totalRead / 1048576.0:0.0} MB";
                        }
                    }
                }

                // Download completed, start background silent installation with admin privileges
                UpdateProgressDetails.Text = "Installing update with administrator privileges in background...";
                UpdateProgressBar.IsIndeterminate = true;
                UpdateStatusText.Text = "• Installing update...";

                var startInfo = new ProcessStartInfo
                {
                    FileName = tempSetupPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    using var proc = Process.Start(startInfo);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusText.Text = $"• Installation cancelled or failed: {ex.Message}";
                    UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                    UpdateProgressPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // Installation finished successfully, show the big tinted restart overlay
                UpdateProgressPanel.Visibility = Visibility.Collapsed;
                UpdateStatusText.Text = $"• Update {tagName} successfully installed!";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;

                UpdateRestartSubtitle.Text = $"Share Bridge has been updated to {tagName}. Restart now to apply the new version.";
                UpdateRestartOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                UpdateStatusText.Text = $"• Up to date (v{currentVersionStr} is the latest version)";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
            }
        }
        catch (HttpRequestException)
        {
            UpdateStatusText.Text = "• Offline: Unable to connect to GitHub. Please check your internet connection.";
            UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            UpdateProgressPanel.Visibility = Visibility.Collapsed;
        }
        catch (TaskCanceledException)
        {
            UpdateStatusText.Text = "• Connection timed out. Please check your internet connection.";
            UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            UpdateProgressPanel.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"• Check failed: {ex.Message}";
            UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            UpdateProgressPanel.Visibility = Visibility.Collapsed;
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void OnRestartAppClick(object sender, RoutedEventArgs e)
    {
        var pfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ShareBridge", "ShareBridge.exe");
        var launchExe = File.Exists(pfPath) ? pfPath : (Environment.ProcessPath ?? "ShareBridge.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = launchExe,
            UseShellExecute = true
        });

        Application.Current.Exit();
    }
}
