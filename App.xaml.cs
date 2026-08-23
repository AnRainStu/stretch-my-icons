using System.Windows;

namespace StretchMyIcons;

public partial class App : System.Windows.Application
{
    private DesktopOverlayHost? overlayHost;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        overlayHost = new DesktopOverlayHost();
        overlayHost.Start();
    }
}
