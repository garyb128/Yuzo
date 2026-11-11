using SQLite;

namespace Yuzo.Models;

// Tells SQLite which "platform" the source came from.
// Windows = a normal file path. Android = a content:// URI.
public enum SourceKind
{
    WindowsFile = 0,
    AndroidContentUri = 1,
}

// [Table("Tracks")] sets the database table name to "Tracks".
// Without this attribute, sqlite-net-pcl would default to the class
// name ("Track", singular). Plural is the SQL convention.
[Table("Tracks")]
public class Track
{
    // ── IDENTITY ─────────────────────────────────────────────────
    // [PrimaryKey] + [AutoIncrement]: SQLite will assign 1, 2, 3, …
    // whenever we insert. We MUST NOT set this ourselves when
    // creating a new Track — leave it at 0 and SQLite fills it in.
    // The ORM writes the assigned Id *back* into the object after
    // InsertAsync, so we know the row's Id immediately.
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // ── SOURCE (what file this track points to) ─────────────────
    // [Indexed(Unique = true)] does TWO things at the SQL level:
    //   1. Creates an index (fast lookup by this column).
    //   2. Enforces that no two rows share this value.
    // The second part is how we prevent importing the same file twice.
    [Indexed(Unique = true)]
    public string SourceUri { get; set; } = "";

    public SourceKind SourceKind { get; set; }

    // ── METADATA (what the file "is") ───────────────────────────
    public string Title { get; set; } = "";
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Composer { get; set; }
    public int? Year { get; set; }
    public int? TrackNumber { get; set; }
    public double DurationSeconds { get; set; }
    public string? ArtworkPath { get; set; }

    // ── APP STATE (things we track over time) ───────────────────
    public bool IsFavourite { get; set; }
    public int PlayCount { get; set; }
    public DateTime? LastPlayed { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    // True if the file has been deleted from disk since import.
    // The library hides these so the UI doesn't show dead tracks.
    public bool IsMissing { get; set; }

    // ── COMPUTED (not stored) ───────────────────────────────────
    // [Ignore] means: do NOT create a column for this.
    // It's a C#-only convenience property for the UI to display.
    [Ignore]
    public string DisplayArtist =>
        string.IsNullOrWhiteSpace(Artist) ? "Unknown artist" : Artist!;

    [Ignore]
    public string DurationLabel
    {
        get
        {
            if (DurationSeconds <= 0) return "";
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return ts.Hours > 0
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    [Ignore]
    public string FavouriteIcon => IsFavourite ? "♥" : "♡";
}