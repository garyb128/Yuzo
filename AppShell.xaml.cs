namespace Yuzo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Shell.SetFlyoutBehavior(
            this,
            Microsoft.Maui.Devices.DeviceInfo.Idiom == Microsoft.Maui.Devices.DeviceIdiom.Desktop
                ? FlyoutBehavior.Locked
                : FlyoutBehavior.Flyout);

        // Register NowPlaying as a pushable route. It's not a primary destination.
        Routing.RegisterRoute("nowplaying", typeof(Views.NowPlayingPage));
    }
}