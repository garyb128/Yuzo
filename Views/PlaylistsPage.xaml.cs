using Yuzo.Services;

namespace Yuzo.Views;

public partial class PlaylistsPage : ContentPage
{
    public PlaylistsPage(PlaybackService playback)
    {
        InitializeComponent();
        PlaybackBarHost.Content = new PlaybackBarView(playback);
    }
}