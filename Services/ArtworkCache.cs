using System.Security.Cryptography;
using System.Text;


namespace Yuzo.Services;

/// <summary>
/// Saves embedded cover art to disk so we don't need to re-read it from
/// the audio file every time the UI needs to show it. Reading a 4MB FLAC
/// to grab a 50KB image is wasteful; caching once fixes that.
/// </summary>
public class ArtworkCache
{
    readonly string _cacheDir;

    public ArtworkCache()
    {
        // All artwork goes in <appdata>/artwork/
        _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "artwork");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Writes the artwork to a file and returns the path.
    /// If the same source key was already cached, reuses the existing file.
    /// </summary>
    public async Task<string?> SaveAsync(byte[] imageBytes, string sourceKey)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return null;

        // Hash the source path so the cached filename is deterministic
        // Same file -> same hash -> same cached path, so a rescan is a no-op
        var hash = Convert.ToHexString(
            SHA1.HashData(Encoding.UTF8.GetBytes(sourceKey)))
            .ToLowerInvariant();

        var path = Path.Combine(_cacheDir, hash + ".jpg");

        if (!File.Exists(path))
            await File.WriteAllBytesAsync(path, imageBytes);

        return path;
    }
}