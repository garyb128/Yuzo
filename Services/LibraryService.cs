using Yuzo.Models;

namespace Yuzo.Services;

// ─────────────────────────────────────────────────────────────
// These two records MUST sit above the class in this same file.
// If they're missing or misspelled, LibraryViewModel won't compile
// because it expects a type named exactly "ImportProgress".
// ─────────────────────────────────────────────────────────────

// Reported once per file during import. The ViewModel subscribes to
// this via IProgress<ImportProgress> and updates the status label.
public record ImportProgress(
    int Processed,
    int Total,
    int Imported,
    int Skipped,
    int Failed,
    string CurrentFile);

// Returned once when the whole import finishes.
public record ImportResult(int Imported, int Skipped, int Failed);

public class LibraryService
{
    private readonly LibraryRepository _repo;
    private readonly TrackMetadataReader _reader;
    private readonly ArtworkCache _artwork;
    private readonly IMusicSource _source;

    public LibraryService(
        LibraryRepository repo,
        TrackMetadataReader reader,
        ArtworkCache artwork,
        IMusicSource source)
    {
        _repo = repo;
        _reader = reader;
        _artwork = artwork;
        _source = source;
    }

    /// <summary>
    /// Full import pipeline: enumerate → dedupe → read metadata →
    /// cache artwork → save. Progress is reported after every file.
    /// </summary>
    public async Task<ImportResult> ImportFolderAsync(
        string folderPath,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var files = await _source.EnumerateAudioFilesAsync(folderPath, ct);

        int imported = 0, skipped = 0, failed = 0;
        int processed = 0;
        int total = files.Count;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            processed++;

            try
            {
                var existing = await _repo.FindBySourceAsync(file);
                if (existing is not null)
                {
                    skipped++;
                    progress?.Report(new ImportProgress(
                        processed, total, imported, skipped, failed, file));
                    continue;
                }

                var read = _reader.Read(file);
                if (read.Track is null)
                {
                    failed++;
                    progress?.Report(new ImportProgress(
                        processed, total, imported, skipped, failed, file));
                    continue;
                }

                var track = read.Track;
                track.SourceUri = file;
                track.SourceKind = SourceKind.WindowsFile;

                if (read.ArtworkBytes is not null)
                {
                    track.ArtworkPath = await _artwork.SaveAsync(
                        read.ArtworkBytes, file);
                }

                await _repo.SaveTrackAsync(track);
                imported++;
            }
            catch
            {
                failed++;
            }

            progress?.Report(new ImportProgress(
                processed, total, imported, skipped, failed, file));
        }

        return new ImportResult(imported, skipped, failed);
    }
}