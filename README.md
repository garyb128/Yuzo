# Yuzo

Yuzo is a local game-soundtrack player built with .NET MAUI. It plays music from your own files. Nothing is streamed, no account is required, and no tracks are bundled with the app.

Windows and Android are the target platforms. On Windows, you import a folder of audio files. Supported types are MP3, FLAC, M4A, AAC, OGG, Opus, and WAV. Tags are read locally and stored in a SQLite library.

## Screens

- **Library** — browse albums and artists, search, and import a folder.
- **Now Playing** — cover art, track details, and playback controls.
- **Quiz** — placeholder. The screen is in the app; the quiz itself is not built yet.
- **Playlists** — placeholder.
- **Settings** — placeholder.

Design notes and the longer roadmap are in [PROGRAM_LAYOUT.md](PROGRAM_LAYOUT.md).

## Build

You need the .NET 11 SDK and the MAUI workload.

```powershell
dotnet workload install maui
dotnet build Yuzo.slnx -f net11.0-windows10.0.19041.0
dotnet run --project Yuzo.csproj -f net11.0-windows10.0.19041.0
```

The Android target is `net11.0-android`. The Windows app is unpackaged (`WindowsPackageType` is `None`).
