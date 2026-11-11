using Yuzo.Services;

namespace Yuzo.Views;

public partial class PlaybackBarView : ContentView
{
    private readonly PlaybackService _playback;

    public PlaybackBarView(PlaybackService playback)
    {
        InitializeComponent();
        _playback = playback;
        BindingContext = playback;
    }

    private void SeekSlider_DragStarted(object? sender, EventArgs e)
        => _playback.IsDragging = true;

    private void SeekSlider_DragCompleted(object? sender, EventArgs e)
    {
        _playback.Seek(SeekSlider.Value);
        _playback.IsDragging = false;
    }
}
