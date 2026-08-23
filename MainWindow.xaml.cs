using System.Windows;

namespace StretchMyIcons;

public partial class MainWindow : Window
{
    private DesktopOverlayHost? overlayHost;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Hide();
        overlayHost = new DesktopOverlayHost();
        overlayHost.Start();
    }
}
