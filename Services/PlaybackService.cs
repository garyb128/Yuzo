using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using Yuzo.Models;

namespace Yuzo.Services;

public partial class PlaybackService : ObservableObject
{
    private readonly IAudioManager _audioManager;
    private readonly LibraryRepository _repository;

    private IAudioPlayer? _player;
    private System.Timers.Timer? _positionTimer;

    public List<Track> Queue { get; private set; } = new();
    public int CurrentIndex { get; private set; } = -1;

    [ObservableProperty] private Track? currentTrack;
    [ObservableProperty] private bool isPlaying;
    [ObservableProperty] private double position;
    [ObservableProperty] private double duration;

    // UI state to stop the timer from fighting the user's slider drag
    [ObservableProperty] private bool isDragging;

    // Auto-generated change notifications for the text labels
    public string PositionText => TimeSpan.FromSeconds(Position).ToString(@"m\:ss");
    public string DurationText => TimeSpan.FromSeconds(Duration).ToString(@"m\:ss");
    public string PlayPauseIcon => IsPlaying ? "Ⅱ" : "▶";

    partial void OnPositionChanged(double value) => OnPropertyChanged(nameof(PositionText));
    partial void OnDurationChanged(double value) => OnPropertyChanged(nameof(DurationText));
    partial void OnIsPlayingChanged(bool value) => OnPropertyChanged(nameof(PlayPauseIcon));

    public PlaybackService(IAudioManager audioManager, LibraryRepository repository)
    {
        _audioManager = audioManager;
        _repository = repository;
    }

    public async Task PlayTrackAsync(Track track, List<Track> queue)
    {
        Queue = queue;
        CurrentIndex = queue.FindIndex(t => t.Id == track.Id);
        if (CurrentIndex < 0) CurrentIndex = 0;

        await LoadAndPlayAsync(track);
    }

    private async Task LoadAndPlayAsync(Track track)
    {
        try
        {
            StopInternal();

            CurrentTrack = track;
            var stream = File.OpenRead(track.SourceUri);
            _player = _audioManager.CreatePlayer(stream);
            _player.PlaybackEnded += Player_PlaybackEnded;

            _player.Play();
            IsPlaying = true;

            // If the DB has 0, fall back to the player's duration.
            Duration = track.DurationSeconds > 0 ? track.DurationSeconds : _player.Duration;

            StartPositionTimer();
            await _repository.RecordPlayAsync(track);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback failed: {ex.Message}");
            CurrentTrack = null;
            IsPlaying = false;
        }
    }

    private void Player_PlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () => await NextAsync());
    }

    public void Pause()
    {
        if (_player != null && _player.IsPlaying)
        {
            _player.Pause();
            IsPlaying = false;
        }
    }

    public void Play()
    {
        if (_player != null && !_player.IsPlaying)
        {
            _player.Play();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (CurrentTrack is null)
            return;

        if (IsPlaying) Pause();
        else Play();
    }

    [RelayCommand]
    private async Task Previous() => await PreviousAsync();

    [RelayCommand]
    private async Task Next() => await NextAsync();

    public void Seek(double seconds)
    {
        if (_player != null && _player.CanSeek)
        {
            _player.Seek(seconds);
            Position = seconds;
        }
    }

    public async Task NextAsync()
    {
        if (Queue.Count == 0) return;
        CurrentIndex = (CurrentIndex + 1) % Queue.Count;
        await LoadAndPlayAsync(Queue[CurrentIndex]);
    }

    public async Task PreviousAsync()
    {
        if (Queue.Count == 0) return;
        CurrentIndex = (CurrentIndex - 1 + Queue.Count) % Queue.Count;
        await LoadAndPlayAsync(Queue[CurrentIndex]);
    }

    private void StopInternal()
    {
        if (_player != null)
        {
            _player.PlaybackEnded -= Player_PlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }
        IsPlaying = false;
        Position = 0;
        Duration = 0;
        _positionTimer?.Stop();
    }

    private void StartPositionTimer()
    {
        _positionTimer?.Stop();
        _positionTimer = new System.Timers.Timer(250); // Update 4 times a second
        _positionTimer.Elapsed += (s, e) =>
        {
            if (_player != null && _player.IsPlaying)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // If the user is currently dragging the slider, don't overwrite their position!
                    if (!IsDragging)
                    {
                        Position = _player.CurrentPosition;
                    }
                });
            }
        };
        _positionTimer.Start();
    }
}