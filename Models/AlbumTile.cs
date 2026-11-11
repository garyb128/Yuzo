namespace Yuzo.Models;

public sealed class AlbumTile
{
    public string Album { get; init; } = "";
    public string Artist { get; init; } = "Unknown artist";
    public int? Year { get; init; }
    public string? ArtworkPath { get; init; }
    public IReadOnlyList<Track> Tracks { get; init; } = Array.Empty<Track>();
    public int TrackCount => Tracks.Count;
    public string YearLabel => Year is int year ? $"{Artist} · {year}" : Artist;
}
