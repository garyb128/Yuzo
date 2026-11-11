using Yuzo.Services;

namespace Yuzo.Views;

public partial class QuizPage : ContentPage
{
    public QuizPage(PlaybackService playback)
    {
        InitializeComponent();
        PlaybackBarHost.Content = new PlaybackBarView(playback);
    }
}