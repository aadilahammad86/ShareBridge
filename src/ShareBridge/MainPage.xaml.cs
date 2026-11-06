using System;
using System.IO;
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

        Loaded += (s, e) => RefreshAdapters();
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

        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ShareBridge-Updater");

            var url = "https://api.github.com/repos/aadilahammad86/ShareBridge/releases/latest";
            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                UpdateStatusText.Text = "• Up to date (v1.0.0 is the latest release)";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
                return;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            if (!string.IsNullOrEmpty(tagName) && !tagName.Equals("v1.0.0", StringComparison.OrdinalIgnoreCase))
            {
                UpdateStatusText.Text = $"• New version {tagName} available!";
                UpdateStatusText.Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush;
                UpdateDownloadLink.Visibility = Visibility.Visible;
            }
            else
            {
                UpdateStatusText.Text = "• Up to date (v1.0.0 is the latest release)";
                UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"• Check failed: {ex.Message}";
            UpdateStatusText.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }
}
