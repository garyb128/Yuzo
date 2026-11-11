// Services/WindowsFolderSource.cs
namespace Yuzo.Services;

public class WindowsFolderSource : IMusicSource
{
    // Filesystem enumeration can freeze the UI if it walks a huge folder.
    // We run it on a thread pool thread via Task.Run (see below).

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".opus", ".wav",
    };

    public Task<IReadOnlyList<string>> EnumerateAudioFilesAsync(
        string folderPath, CancellationToken ct = default)
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var results = new List<string>();

            // SearchOption.AllDirectories means "recurse into subfolders".
            // Directory.EnumerateFiles is lazy — it yields files as it walks,
            // which is why we wrap in Task.Run (the walk happens off-thread).
            foreach (var file in Directory.EnumerateFiles(
                folderPath, "*.*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var ext = Path.GetExtension(file);
                if (SupportedExtensions.Contains(ext))
                    results.Add(file);
            }

            return results;
        }, ct);
    }
}