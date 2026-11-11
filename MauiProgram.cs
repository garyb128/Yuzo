using CommunityToolkit.Maui;               // brings UseMauiCommunityToolkit()
using Microsoft.Extensions.Logging;        // brings AddDebug()
using Plugin.Maui.Audio;
using Yuzo.ViewModels;
using Yuzo.Views;

// This alias is the fix for the "ambiguous call" error.
// It says: when I write "MSDI.", I mean Microsoft's DI extensions class,
// not the same-named method that CommunityToolkit.Maui also provides.
using MSDI = Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions;

namespace Yuzo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()      // CommunityToolkit.Maui — no ambiguity, no other UseMauiCommunityToolkit exists
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var services = builder.Services;

        // ───── Pages ─────
        // Tab pages: Singleton, they live once for the app's lifetime.
        MSDI.AddSingleton<LibraryPage>(services);
        MSDI.AddSingleton<QuizPage>(services);
        MSDI.AddSingleton<PlaylistsPage>(services);
        MSDI.AddSingleton<SettingsPage>(services);

        // Now Playing is pushed/popped — Transient means a new one each time.
        MSDI.AddTransient<NowPlayingPage>(services);

        // ───── ViewModels ─────
        // Transient so each page gets its own copy (no leaking state between pages).
        MSDI.AddTransient<LibraryViewModel>(services);
        MSDI.AddTransient<QuizViewModel>(services);
        MSDI.AddTransient<PlaylistsViewModel>(services);
        MSDI.AddTransient<SettingsViewModel>(services);
        MSDI.AddTransient<NowPlayingViewModel>(services);

        // Services
        MSDI.AddSingleton<Services.LibraryRepository>(services);
        MSDI.AddSingleton<Services.TrackMetadataReader>(services);
        MSDI.AddSingleton<Services.ArtworkCache>(services);
        MSDI.AddSingleton<Services.LibraryService>(services);
        MSDI.AddSingleton<Services.IMusicSource, Services.WindowsFolderSource>(services);
        MSDI.AddSingleton<Services.PlaybackService>(services);
        MSDI.AddSingleton<IAudioManager>(services,AudioManager.Current);

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}