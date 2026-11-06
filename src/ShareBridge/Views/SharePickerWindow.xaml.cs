using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShareBridge.Adapters.BrowserApps;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Adapters.Telegram;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;
using ShareBridge.Core.Services;

namespace ShareBridge.Views;

public sealed partial class SharePickerWindow : Window
{
    private readonly ShareOperation? _shareOperation;
    private readonly IShareDataExtractor _extractor;
    private readonly IAdapterRegistry _registry;
    private SharePayload? _payload;
    private readonly string[]? _directFiles;

    public SharePickerWindow(ShareOperation? shareOperation = null, string[]? directFiles = null)
    {
        InitializeComponent();
        _shareOperation = shareOperation;
        _directFiles = directFiles;

        var tempManager = new TempFileManager();
        _extractor = new ShareDataExtractor(tempManager);
        _registry = new AdapterRegistry();

        // Register default adapters
        _registry.RegisterAdapter(new TelegramAdapter());
        _registry.RegisterAdapter(new ClipboardAdapter());

        // Discover and register browser shortcut apps (WhatsApp Web, Gmail, etc.)
        var detector = new BrowserAppDetector();
        foreach (var app in detector.DetectInstalledApps())
        {
            _registry.RegisterAdapter(new BrowserAppAdapter(app));
        }

        ConfigureWindowSize();
        _ = LoadPayloadAndPopulateUiAsync();
    }

    private void ConfigureWindowSize()
    {
        var appWindow = AppWindow;
        if (appWindow != null)
        {
            appWindow.Resize(new SizeInt32(460, 540));
        }
    }

    private async Task LoadPayloadAndPopulateUiAsync()
    {
        if (_shareOperation != null)
        {
            try
            {
                _shareOperation.ReportStarted();
                _payload = await _extractor.ExtractAsync(_shareOperation.Data);
            }
            catch (Exception ex)
            {
                PreviewTitle.Text = "Error reading share data";
                PreviewDetail.Text = ex.Message;
                return;
            }
        }
        else if (_directFiles != null && _directFiles.Length > 0)
        {
            _payload = new SharePayload
            {
                Type = PayloadType.Files,
                FilePaths = new System.Collections.Generic.List<string>(_directFiles)
            };
        }
        else
        {
            // Demo / Standalone fallback mode
            _payload = new SharePayload
            {
                Type = PayloadType.Text,
                Text = "Sample shared content from Windows 11"
            };
        }

        UpdatePreviewCard(_payload);
        PopulateAdapters(_payload);
    }

    private void UpdatePreviewCard(SharePayload payload)
    {
        if (payload.Type.HasFlag(PayloadType.Files) && payload.FilePaths.Count > 0)
        {
            PreviewIcon.Glyph = "\uE8B7"; // File
            var count = payload.FilePaths.Count;
            if (count == 1)
            {
                var file = payload.FilePaths[0];
                var name = Path.GetFileName(file);
                PreviewTitle.Text = TruncateMiddle(name, 34);
                try
                {
                    var bytes = new FileInfo(file).Length;
                    PreviewDetail.Text = FormatFileSize(bytes);
                }
                catch
                {
                    PreviewDetail.Text = "1 file";
                }
            }
            else
            {
                PreviewTitle.Text = $"{count} files";
                long totalBytes = 0;
                foreach (var file in payload.FilePaths)
                {
                    try { totalBytes += new FileInfo(file).Length; } catch { }
                }
                PreviewDetail.Text = $"{count} items • {FormatFileSize(totalBytes)}";
            }
        }
        else if (payload.Type.HasFlag(PayloadType.WebLink) && payload.WebLink != null)
        {
            PreviewIcon.Glyph = "\uE774"; // Globe
            PreviewTitle.Text = payload.WebLink.ToString();
            PreviewDetail.Text = payload.WebLink.Host;
        }
        else if (payload.Type.HasFlag(PayloadType.Text) && !string.IsNullOrWhiteSpace(payload.Text))
        {
            PreviewIcon.Glyph = "\uE8BD"; // Message
            var singleLine = payload.Text.Replace("\r", " ").Replace("\n", " ");
            PreviewTitle.Text = TruncateEnd(singleLine, 45);
            PreviewDetail.Text = $"{payload.Text.Length} characters";
        }
        else
        {
            PreviewIcon.Glyph = "\uE9CE"; // Unknown
            PreviewTitle.Text = "Unknown content";
            PreviewDetail.Text = "No recognized formats";
        }
    }

    private void PopulateAdapters(SharePayload payload)
    {
        AdaptersPanel.Children.Clear();
        var compatible = _registry.GetCompatibleAdapters(payload);

        if (compatible.Count == 0)
        {
            AdaptersPanel.Children.Add(new TextBlock
            {
                Text = "No compatible applications found for this content.",
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return;
        }

        foreach (var adapter in compatible)
        {
            var btn = CreateAdapterButton(adapter, payload);
            AdaptersPanel.Children.Add(btn);
        }
    }

    private Button CreateAdapterButton(ITargetAdapter adapter, SharePayload payload)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 2, 0, 2),
            BorderThickness = new Thickness(1)
        };

        var contentGrid = new Grid { ColumnSpacing = 12 };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon
        var icon = new FontIcon
        {
            Glyph = adapter.IconGlyph ?? "\uE71D",
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush
        };
        Grid.SetColumn(icon, 0);
        contentGrid.Children.Add(icon);

        // Text
        var textStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
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
        contentGrid.Children.Add(textStack);

        // Chevron
        var chevron = new FontIcon
        {
            Glyph = "\uE76C", // ChevronRight
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] as Brush
        };
        Grid.SetColumn(chevron, 2);
        contentGrid.Children.Add(chevron);

        button.Content = contentGrid;

        button.Click += async (s, e) =>
        {
            button.IsEnabled = false;
            ActionProgressBar.Visibility = Visibility.Visible;

            try
            {
                var result = await adapter.SendAsync(payload);
                if (result.Success)
                {
                    _shareOperation?.ReportCompleted();
                    Close();
                }
                else
                {
                    ActionProgressBar.Visibility = Visibility.Collapsed;
                    button.IsEnabled = true;
                    PreviewDetail.Text = result.ErrorMessage ?? "Transfer failed.";
                }
            }
            catch (Exception ex)
            {
                ActionProgressBar.Visibility = Visibility.Collapsed;
                button.IsEnabled = true;
                PreviewDetail.Text = ex.Message;
            }
        };

        return button;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _shareOperation?.ReportCompleted();
        Close();
    }

    private static string TruncateMiddle(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        int half = (maxLength - 3) / 2;
        return $"{text[..half]}...{text[^half..]}";
    }

    private static string TruncateEnd(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return $"{text[..(maxLength - 3)]}...";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }
}
