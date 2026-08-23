using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingIcon = System.Drawing.Icon;

namespace StretchMyIcons;

public partial class MainWindow : Window
{
    private const double TileSize = 76;
    private const double Gap = 8;
    private int columns = 1;
    private int rows = 1;
    private readonly List<ShortcutItem> shortcuts = [];

    public MainWindow()
    {
        InitializeComponent();
        LoadShortcuts();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        RenderTiles();
    }

    private void LoadShortcuts()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        foreach (var path in Directory.EnumerateFiles(desktop).Concat(Directory.EnumerateFiles(publicDesktop)))
        {
            if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".url", StringComparison.OrdinalIgnoreCase)) continue;
            shortcuts.Add(new ShortcutItem(Path.GetFileNameWithoutExtension(path), path));
        }
    }

    private void RenderTiles()
    {
        TilePanel.Children.Clear();
        foreach (var item in shortcuts.Take(24))
        {
            var tile = new System.Windows.Controls.Button
            {
                Width = columns * TileSize + (columns - 1) * Gap,
                Height = rows * TileSize + (rows - 1) * Gap,
                Margin = new Thickness(0, 0, Gap, Gap),
                Padding = new Thickness(8),
                Background = (System.Windows.Media.Brush)FindResource("TileBrush"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"),
                BorderThickness = new Thickness(0),
                ToolTip = item.Name,
                Tag = item.Path
            };
            var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var icon = TryGetIcon(item.Path);
            if (icon is not null) content.Children.Add(new System.Windows.Controls.Image { Source = icon, Width = 32, Height = 32, Margin = new Thickness(0, 0, 0, 5) });
            content.Children.Add(new TextBlock { Text = item.Name, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxHeight = 34 });
            tile.Content = content;
            tile.Click += TileClicked;
            TilePanel.Children.Add(tile);
        }
        UpdateLayout();
        PositionAtBottomLeft();
    }

    private static ImageSource? TryGetIcon(string path)
    {
        try
        {
            using var icon = DrawingIcon.ExtractAssociatedIcon(path);
            if (icon is null) return null;
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
        }
        catch { return null; }
    }

    private void TileClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string path })
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void GridChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridPicker.SelectedItem is ComboBoxItem { Tag: string value })
        {
            var parts = value.Split(',');
            columns = int.Parse(parts[0]);
            rows = int.Parse(parts[1]);
            RenderTiles();
        }
    }

    private void PositionAtBottomLeft()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + 14;
        Top = workArea.Bottom - ActualHeight - 14;
    }

    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
}

internal sealed record ShortcutItem(string Name, string Path);
