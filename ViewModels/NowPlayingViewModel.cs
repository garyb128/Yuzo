using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yuzo.Services;

namespace Yuzo.ViewModels;

public partial class NowPlayingViewModel : ObservableObject
{
    // Expose the whole service so the XAML can bind to Playback.CurrentTrack, etc.
    public PlaybackService Playback { get; }

    public NowPlayingViewModel(PlaybackService playback)
    {
        Playback = playback;
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (Playback.IsPlaying) Playback.Pause();
        else Playback.Play();
    }

    [RelayCommand]
    private async Task Next() => await Playback.NextAsync();

    [RelayCommand]
    private async Task Previous() => await Playback.PreviousAsync();

    [RelayCommand]
    private async Task Close() => await Shell.Current.GoToAsync(".."); // Go back
}