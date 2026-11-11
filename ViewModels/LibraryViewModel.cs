using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yuzo.Models;
using Yuzo.Services;

namespace Yuzo.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly LibraryRepository _repository;
    private readonly LibraryService _library;

    private readonly PlaybackService _playback;

    public LibraryViewModel(LibraryRepository repository, LibraryService library, PlaybackService playback)
    {
        _repository = repository;
        _library = library;
        _playback = playback;
    }

    // ── Data ──────────────────────────────────────────────────────
    // Tracks holds every visible track from the DB. FilteredTracks is
    // what the CollectionView binds to — it's the subset matching the
    // current search + favourite filter.
    public ObservableCollection<Track> Tracks { get; } = new();
    public ObservableCollection<Track> FilteredTracks { get; } = new();
    public ObservableCollection<AlbumTile> FilteredAlbums { get; } = new();
    public ObservableCollection<ArtistTile> FilteredArtistTiles { get; } = new();
    public ObservableCollection<object> VisibleItems { get; } = new();

    // ── UI state ──────────────────────────────────────────────────
    [ObservableProperty]
    private string statusMessage = "Ready";

    private string _selectedView = "Albums";

    public string SelectedView
    {
        get => _selectedView;
        private set
        {
            if (_selectedView == value)
                return;

            _selectedView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAlbumsView));
            OnPropertyChanged(nameof(IsArtistsView));
            OnPropertyChanged(nameof(IsSongsView));
            UpdateVisibleItems();
        }
    }

    public bool IsAlbumsView => SelectedView == "Albums";
    public bool IsArtistsView => SelectedView == "Artists";
    public bool IsSongsView => SelectedView == "Songs";

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private bool showFavouritesOnly;

    [ObservableProperty]
    private bool isImporting;

    // Partial methods automatically called by the source generator
    // every time the associated [ObservableProperty] changes.
    // This is how we react to search-as-you-type without extra wiring.
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowFavouritesOnlyChanged(bool value) => ApplyFilter();

    // ── Commands ──────────────────────────────────────────────────
    [RelayCommand]
    private void SelectView(string view)
    {
        if (view is "Albums" or "Artists" or "Songs")
            SelectedView = view;
    }

    [RelayCommand]
    private void ToggleFavouritesFilter()
        => ShowFavouritesOnly = !ShowFavouritesOnly;

    [RelayCommand]
    private void ClearSearch()
        => SearchText = "";

    [RelayCommand]
    private async Task ImportFolder()
    {
        if (IsImporting) return;

        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (!result.IsSuccessful)
            {
                StatusMessage = "No folder selected.";
                return;
            }

            IsImporting = true;
            StatusMessage = $"Scanning {result.Folder.Path}…";

            var progress = new Progress<ImportProgress>(p =>
            {
                StatusMessage = p.Total > 0
                    ? $"{p.Processed}/{p.Total} — imported {p.Imported}, " +
                      $"skipped {p.Skipped}, failed {p.Failed}"
                    : $"Imported {p.Imported}";
            });

            var importResult = await _library.ImportFolderAsync(
                result.Folder.Path, progress, CancellationToken.None);

            await ReloadTracksAsync();

            StatusMessage =
                $"Done. Imported {importResult.Imported}, " +
                $"skipped {importResult.Skipped}, failed {importResult.Failed}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        if (IsImporting) return;
        await _repository.DeleteAllTracksAsync();
        await ReloadTracksAsync();
        StatusMessage = "All tracks cleared.";
    }

    // ── Loading + filtering ──────────────────────────────────────
    private async Task ReloadTracksAsync()
    {
        var all = await _repository.GetAllTracksAsync();
        Tracks.Clear();
        foreach (var t in all) Tracks.Add(t);

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Track> query = Tracks;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            query = query.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (t.Artist?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Album?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (ShowFavouritesOnly)
            query = query.Where(t => t.IsFavourite);

        var matchingTracks = query.ToList();
        FilteredTracks.Clear();
        foreach (var track in matchingTracks)
            FilteredTracks.Add(track);

        RebuildAlbumTiles(matchingTracks);
        RebuildArtistTiles(matchingTracks);
        UpdateVisibleItems();
    }

    private void UpdateVisibleItems()
    {
        IEnumerable<object> items = SelectedView switch
        {
            "Artists" => FilteredArtistTiles,
            "Songs" => FilteredTracks,
            _ => FilteredAlbums,
        };

        VisibleItems.Clear();
        foreach (var item in items)
            VisibleItems.Add(item);
    }

    private void RebuildAlbumTiles(IEnumerable<Track> tracks)
    {
        var albums = tracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Album))
            .GroupBy(track => track.Album!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var albumTracks = group.OrderBy(track => track.TrackNumber ?? int.MaxValue).ToList();
                var first = albumTracks[0];
                var albumArtists = albumTracks
                    .Select(track => track.Artist)
                    .Where(artist => !string.IsNullOrWhiteSpace(artist))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var albumArtist = albumArtists.Count switch
                {
                    0 => "Unknown artist",
                    1 => albumArtists[0]!,
                    _ => "Various artists",
                };
                var year = albumTracks
                    .Where(track => track.Year.HasValue)
                    .GroupBy(track => track.Year!.Value)
                    .OrderByDescending(years => years.Count())
                    .Select(years => (int?)years.Key)
                    .FirstOrDefault();

                return new AlbumTile
                {
                    Album = first.Album!.Trim(),
                    Artist = albumArtist,
                    Year = year,
                    ArtworkPath = albumTracks.FirstOrDefault(track => !string.IsNullOrWhiteSpace(track.ArtworkPath))?.ArtworkPath,
                    Tracks = albumTracks,
                };
            })
            .OrderBy(album => album.Album, StringComparer.OrdinalIgnoreCase)
            .ToList();

        FilteredAlbums.Clear();
        foreach (var album in albums)
            FilteredAlbums.Add(album);
    }

    private void RebuildArtistTiles(IEnumerable<Track> tracks)
    {
        var artists = tracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Artist))
            .GroupBy(track => track.Artist!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ArtistTile
            {
                Artist = group.First().Artist!,
                ArtworkPath = group.FirstOrDefault(track => !string.IsNullOrWhiteSpace(track.ArtworkPath))?.ArtworkPath,
                TrackCount = group.Count(),
            })
            .OrderBy(artist => artist.Artist, StringComparer.OrdinalIgnoreCase)
            .ToList();

        FilteredArtistTiles.Clear();
        foreach (var artist in artists)
            FilteredArtistTiles.Add(artist);
    }

    [RelayCommand]
    private async Task PlayAlbum(AlbumTile album)
    {
        if (album?.Tracks.FirstOrDefault() is not Track firstTrack)
            return;

        var albumTracks = album.Tracks.ToList();
        await _playback.PlayTrackAsync(firstTrack, albumTracks);
        await Shell.Current.GoToAsync("nowplaying");
    }

    [RelayCommand]
    private async Task SelectTrack(Track track)
    {
        if (track is null) return;

        // Pass the current filtered list so the queue matches what you see on screen
        await _playback.PlayTrackAsync(track, FilteredTracks.ToList());

        // Navigate to the NowPlaying page
        await Shell.Current.GoToAsync("nowplaying");
    }

    [RelayCommand]
    private async Task ToggleFavourite(Track track)
    {
        if (track is null) return;

        // Flip the boolean
        track.IsFavourite = !track.IsFavourite;

        // Save it to the database
        await _repository.SaveTrackAsync(track);

        // If looking at favourites, refresh the list
        ApplyFilter();
    }

    public async Task OnAppearingAsync() => await ReloadTracksAsync();
}