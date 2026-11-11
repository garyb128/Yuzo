using Yuzo.ViewModels;

namespace Yuzo.Views;

public partial class NowPlayingPage : ContentPage
{
    public NowPlayingPage(NowPlayingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void SeekSlider_DragStarted(object sender, EventArgs e)
    {
        if (BindingContext is NowPlayingViewModel vm)
        {
            // Tell the timer to stop updating the slider while we drag
            vm.Playback.IsDragging = true;
        }
    }

    private void SeekSlider_DragCompleted(object sender, EventArgs e)
    {
        if (sender is Slider slider && BindingContext is NowPlayingViewModel vm)
        {
            // Seek to the new position
            vm.Playback.Seek(slider.Value);

            // Let the timer take over again
            vm.Playback.IsDragging = false;
        }
    }
}