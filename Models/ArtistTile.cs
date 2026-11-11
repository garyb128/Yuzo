namespace Yuzo.Models
{
    // A lightweight object grouping tracks by artist for the horizontal
    // tiles strip at the top of the Library page. Not a database entity —
    // built on the fly from the Track collection each time it reloads.
    public class ArtistTile
    {
        public string Artist { get; set; } = "";
        public string? ArtworkPath { get; set; }
        public int TrackCount { get; set; }
    }
}
