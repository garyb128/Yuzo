// Services/IMusicSource.cs
namespace Yuzo.Services;

/// <summary>
/// Abstracts "where do files come from". On Windows it's a folder scan;
/// on Android it'll be a MediaStore query. The rest of the app doesn't
/// care — it just asks for audio files and gets paths/URIs back.
/// </summary>
public interface IMusicSource
{
    Task<IReadOnlyList<string>> EnumerateAudioFilesAsync(
        string folderPath, CancellationToken ct = default);
}