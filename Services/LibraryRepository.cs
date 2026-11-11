using SQLite;
using Yuzo.Models;

namespace Yuzo.Services;

public class LibraryRepository
{
    // The connection object - our handle to the database file
    // Null until the first call to GetDbAsync(), then cached forever
    SQLiteAsyncConnection? _db;

    // Guards against two concurrent calls both trying to open
    // and create the DB at startup. Prevents duplicate work.
    readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Opens the database (creating it if needed) and ensures the schema exists.
    /// Call this once; subsequent calls return the cached connection instantly.
    /// </summary>
    async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null)
            return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null)
                return _db;

            // FileSystem.AppDataDirectory is a MAUI-provided path that is
            // guaranteed writable by the app on every platform:
            //   Windows: %LOCALAPPDATA%\Yuzo\...\LocalState\
            //   Android: /data/data/com.garyb128.yuzo/files/
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "yuzo.db3");

            _db = new SQLiteAsyncConnection(
                 dbPath,
                 SQLiteOpenFlags.ReadWrite     // we need to read AND write
                 | SQLiteOpenFlags.Create      // create the file if missing
                 | SQLiteOpenFlags.SharedCache);

            // Creates every table listed that doesn't already exit
            await _db.CreateTableAsync<Track>();

            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Insert a new track, or update an existing one.
    /// A track is considered "new" if its Id is still 0 (never saved).
    /// </summary>
    public async Task<int> SaveTrackAsync(Track track)
    {
        var db = await GetDbAsync();

        if (track.Id == 0)
            await db.InsertAsync(track); // INSERT - SQLite assings Id
        else
            await db.UpdateAsync(track); // UPDATE - matches existing by Id

        return track.Id;
    }

    /// <summary>
    /// Returns every non-missing track, sorted for display.
    /// </summary>
    public async Task<List<Track>> GetAllTracksAsync()
    {
        var db = await GetDbAsync();

        return await db.Table<Track>()          // SELECT * FROM Tracks
                       .Where(t => !t.IsMissing) // WHERE IsMissing = 0
                       .OrderBy(t => t.Artist)   // ORDER BY Artist, Album, TrackNumber
                       .ThenBy(t => t.Album)
                       .ThenBy(t => t.TrackNumber)
                       .ToListAsync();           // ← THIS is where the SQL actually runs
    }

    /// <summary>
    /// Find a track by its unique source path/URI.
    /// Returns null if no row matches. Used during import for dedupe.
    /// </summary>
    public async Task<Track?> FindBySourceAsync(string sourceUri)
    {
        var db = await GetDbAsync();
        return await db.Table<Track>()
                       .Where(t => t.SourceUri == sourceUri)
                       .FirstOrDefaultAsync();
    }

    /// <summary>Deletes every track. Use for dev / "start fresh".</summary>
    public async Task DeleteAllTracksAsync()
    {
        var db = await GetDbAsync();
        await db.ExecuteAsync("DELETE FROM Tracks");
    }

    public async Task RecordPlayAsync(Track track)
    {
        track.PlayCount++;
        track.LastPlayed = DateTime.UtcNow;
        await SaveTrackAsync(track);
    }
}