# Yuzo — Program Layout & Roadmap

## Overview

**Yuzo** is a local game soundtrack player with a built-in quiz, for **Windows and Android**.

- No web APIs, accounts or telemetry
- Music comes from the user's own files
- Windows imports folders directly
- Android imports from the device audio library via MediaStore
- Quiz mode is part of v1, not an optional extra
- Fan-made, non-commercial project. No music is bundled with the app.

**Project location:** `C:\Users\gbran\Documents\Work\Personal\Yuzo`  
**Package ID:** `com.garyb128.yuzo`  
**Target frameworks:** `net10.0-windows10.0.19041.0`, `net10.0-android`

---

## Product principles

1. **Local first** — no network calls are required for any feature.
2. **Fast library browsing** — scanning and metadata parsing must not block the UI.
3. **Metadata is editable** — local tags are often messy, especially the game name.
4. **Quiz is a first-class feature** — it must ship in v1.
5. **Platform-native storage** — Windows uses paths; Android uses content URIs.

---

## Screens

### Library

- Search by track, game or composer
- Filters: all tracks, games, favourites
- Grouped list: game → tracks
- Track row: cover, title, game, duration, favourite indicator
- Actions: **Add music / Scan**, **Rescan**

### Now Playing

- Large cover art
- Track title, game, composer and year
- Seek bar with elapsed/remaining time
- Play/pause, previous, next
- Volume
- Favourite and add-to-playlist actions

### Quiz

- **Setup:** mode, clip length, number of questions
- **Round:** hidden track info, clip playback, four answer options
- **Result:** correct/wrong feedback, reveal answer, next round
- **Summary:** score, best streak, replay wrong answers

### Playlists

- List of playlists
- Create, rename and delete
- Playlist detail with track list
- Reorder and remove tracks

### Settings

- Music source: add folder (Windows) or scan device (Android)
- Rescan library and remove missing tracks
- Quiz defaults: clip length, question count
- About Yuzo

A persistent mini-player is a later polish item.

---

## Architecture

```
Views / XAML
    ↓
ViewModels
    ↓
Services
    ├── LibraryService
    ├── PlaybackService
    ├── QuizService
    ├── PlaylistService
    └── LibraryRepository → SQLite
            ↑
    IMusicSource (platform-specific)
        ├── Windows folder scanner
        └── Android MediaStore scanner
```

### Dependency direction

- Views know ViewModels and bindings only.
- ViewModels call services.
- Services own database, files and playback.
- Platform-specific implementations sit behind interfaces.
- No service reaches into XAML or page code.

---

## Data model

### Track

- Id
- Source URI or file path
- Source kind: Windows file / Android content URI
- Title
- Game title
- Album
- Composer
- Year
- Track number
- Duration
- Artwork path
- Is favourite
- Play count
- Last played
- Date added
- Is missing

### Playlist

- Id
- Name
- Created date

### PlaylistTrack

- Playlist id
- Track id
- Position

### QuizResult

- Id
- Mode
- Score
- Total questions
- Best streak
- Played date

For v1, the game is stored as a string on the track (`GameTitle`). A separate Game table can come later if the library grows enough to need it.

---

## Services

### IMusicSource

Platform-specific library source.

- **Windows:** pick a folder, recurse through supported files
- **Android:** query MediaStore, including permission handling

### TrackMetadataReader

- Wraps TagLibSharp
- Reads title, game/album, composer, year, track number, duration and pictures
- Works from a file path on Windows and a stream on Android

### ArtworkCache

- Extracts embedded cover art
- Writes it to app data
- Stores the cached path on the track

### LibraryRepository

- SQLite via `sqlite-net-pcl`
- CRUD for tracks, playlists and quiz results
- Duplicate detection by source URI/path
- Missing-track handling

### IAudioPlayer

- Load track
- Play, pause, stop
- Seek
- Volume
- Position and state events

### PlaybackQueue

- Current track
- Next/previous
- Queue source: library, playlist or quiz
- Shuffle/repeat later

### QuizService

- Build random quiz sessions
- Generate multiple-choice options
- Track score, streak and used tracks
- Guard against tracks without game metadata

### PlaylistService

- Create and edit playlists
- Add/remove tracks
- Keep track order

### SettingsService

- Store library sources and quiz defaults
- Use MAUI `Preferences`

---

## Packages

- `CommunityToolkit.Mvvm` — MVVM source generators
- `CommunityToolkit.Maui` — folder picker and UI helpers
- `sqlite-net-pcl` — local database
- `TagLibSharp` — audio metadata and cover art
- `Plugin.Maui.Audio` — audio playback on Windows and Android

No external service SDKs.

---

## Platform strategy

### Windows

- Pick a folder with the folder picker
- Recursively scan supported audio extensions
- Read metadata and embedded artwork
- Store the file path
- Play from a file stream
- Re-scan without duplicating existing tracks

### Android

- Use **MediaStore**, not raw folder paths
- Request `READ_MEDIA_AUDIO` on Android 13+
- Request `READ_EXTERNAL_STORAGE` on older versions
- Store the content URI
- Open streams through the content resolver for metadata and playback
- Handle revoked permissions and missing files gracefully

### Supported formats in v1

MP3, FLAC, M4A/AAC, OGG, OPUS and WAV, where the platform audio stack supports them. (possibly other formats in the future)

**Not in v1:** NSF, SPC, VGM, PSF and other game-music formats. Those require format-specific emulation/decoding and are a separate project.

---

## Quiz design

### Modes

- **Guess the game** — v1
- Guess the track title — v1.1
- Guess the composer — later

### Round flow

1. Pick a random track with a known game.
2. Start at a random point in the track.
3. Play a 5, 10 or 20-second clip.
4. Show four game options.
5. Reveal the answer.
6. Award a point for a correct answer.
7. Track the current streak.
8. Continue until the question count is reached.
9. Show a summary with score and best streak.

### Rules

- Do not repeat the same track within a session.
- Multiple-choice options must be distinct games.
- If the library has fewer than four games:
  - Use two or three options when possible.
  - Otherwise fall back to free-text entry.
- Tracks without a game title are excluded from the quiz.
- Clips start away from the very beginning where possible, to avoid intros and silence.

### Metadata editor

Required for a good quiz:

- Edit title, game, composer, year and track number
- Save changes to SQLite
- Optionally write changes back to file tags later
- Mark tracks as missing or ignored

---

## Project structure

```
Yuzo/
  App.xaml
  App.xaml.cs
  AppShell.xaml
  AppShell.xaml.cs
  MauiProgram.cs
  Models/
  Services/
  ViewModels/
  Views/
  Platforms/
    Android/
    Windows/
  Resources/
```

---

## Roadmap

### Phase 0 — Skeleton

- Create the MAUI project at `C:\Users\gbran\Documents\Work\Personal\Yuzo`
- Windows and Android target frameworks
- DI and Shell tabs
- Base theme and empty pages
- Add packages
- Verify both target builds

### Phase 1 — Data layer

- Track, playlist and quiz-result models
- SQLite repository
- Database creation and CRUD
- Seed/sample data so UI work can begin before scanning exists

### Phase 2 — Windows import

- Folder picker
- Recursive scan of supported extensions
- TagLib metadata and cover art
- Progress reporting
- Duplicate detection by path
- Persist tracks
- Handle unreadable or corrupt files

### Phase 3 — Library UI

- Grouped list by game
- Search and favourites
- Track row design
- Empty, loading and error states
- Sort by title, game or date added

### Phase 4 — Playback

- Windows audio playback
- Play/pause, seek and volume
- Previous/next and queue
- Now Playing screen
- Persist play count and last played

### Phase 5 — Metadata editor

- Edit game/title/composer/year
- Fix missing game metadata
- Prepare the library for the quiz

### Phase 6 — Quiz

- Quiz setup: mode, clip length, question count
- Guess-the-game rounds
- Clip playback and four answer options
- Score, streak and results
- Quiz defaults in Settings
- Replay wrong answers

### Phase 7 — Playlists and favourites

- Favourite tracks
- Create and edit playlists
- Add/remove tracks
- Play a playlist
- Reorder tracks

### Phase 8 — Android

- MediaStore scan
- Runtime permissions
- Content-URI metadata and artwork
- Content-URI playback
- Verify player and quiz on Android

### Phase 9 — Polish

- App icon and splash screen
- Dark/light theme polish
- README and screenshots
- Repository and quiz tests
- Release builds for Windows and Android

---

## v1 definition

Yuzo v1 is done when:

- Windows and Android can scan local music
- The library is grouped by game
- Tracks play with seek, volume and next/previous
- Game metadata can be corrected in the app
- Quiz mode works with multiple choice, clip lengths, score and streaks
- Favourites and basic playlists work
- No network access is required

---

## Later

- Guess the track title and composer modes
- Persistent mini-player
- Collection statistics
- Equalizer and visualizer
- NSF/VGM/SPC support
- Write metadata edits back to file tags
- Shuffle and repeat modes
- Import/export playlists

---

## Main risks

1. **Android storage** — MediaStore and permissions require platform-specific code.
2. **Metadata quality** — many files will have no usable game name; the editor is essential.
3. **Large libraries** — scanning and artwork extraction must not freeze the UI.
4. **Quiz options** — libraries with fewer than four games need a fallback.
5. **Audio formats** — support depends on Windows MediaPlayer and Android MediaPlayer.

---

## First step

Start with **Phase 0**: create the solution, add the packages, set up Shell tabs, and confirm Windows and Android builds.

After that, build the data layer and Windows import before touching the quiz.
