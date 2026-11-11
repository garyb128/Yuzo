using Yuzo.Services;

namespace Yuzo.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(PlaybackService playback)
    {
        InitializeComponent();
        PlaybackBarHost.Content = new PlaybackBarView(playback);
    }
}