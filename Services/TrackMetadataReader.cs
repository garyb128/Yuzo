using Yuzo.Models;

namespace Yuzo.Services;

// Return type for Read(). Using a record because it groups three
// related values cleanly without needing out-parameters.
// - Track: the metadata, or null if the file couldn't be read
// - ArtworkBytes: the raw JPEG/PNG bytes of the first embedded picture, or null
// - Error: human-readable failure reason, or null if success
public record MetadataReadResult(Track? Track, byte[]? ArtworkBytes, string? Error);

public class TrackMetadataReader
{
    /// <summary>
    /// Reads tags and embedded artwork from a single audio file.
    /// Returns a result with Track=null on failure — never throws.
    /// </summary>
    public MetadataReadResult Read(string filePath)
    {
        try
        {
            // TagLib.File.Create parses the file and returns a handle.
            // Note the fully-qualified "TagLib.File" — the bare name "File"
            // would collide with System.IO.File, so we qualify it.
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            var track = new Track
            {
                // Fallback: if the file has no Title tag (very common),
                // use the filename without extension. Better than "Unknown".
                Title = !string.IsNullOrWhiteSpace(tag.Title)
                    ? tag.Title.Trim()
                    : Path.GetFileNameWithoutExtension(filePath),

                // Artists are stored weirdly in tags. Try the "performer"
                // tag first (most common), then the "album artist" tag.
                Artist = FirstNonEmpty(tag.FirstPerformer, tag.FirstAlbumArtist),

                Album = NullIfBlank(tag.Album),
                Composer = NullIfBlank(tag.FirstComposer),

                // TagLib returns 0 for "no year recorded", so convert to null.
                Year = tag.Year > 0 ? (int)tag.Year : null,
                TrackNumber = tag.Track > 0 ? (int)tag.Track : null,

                // Duration comes from the audio stream itself, not the tags.
                // file.Properties.Duration is a TimeSpan.
                DurationSeconds = file.Properties?.Duration.TotalSeconds ?? 0
            };

            // Embedded artwork: files can have multiple pictures (front cover,
            // back, artist photo…). We take the first one, usually the cover.
            var artworkBytes = tag.Pictures?.FirstOrDefault()?.Data?.Data;

            return new MetadataReadResult(track, artworkBytes, null);
        }
        catch (Exception ex)
        {
            // Corrupt file, unsupported codec, permission denied — all end up here.
            return new MetadataReadResult(null, null, ex.Message);
        }
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}