namespace Api.Services.Storage;

public interface IObjectStorageService
{
    /// <summary>
    /// Uploads (or overwrites) a UTF-8 text object at the given key.
    /// </summary>
    Task PutTextAsync(string key, string content, CancellationToken ct = default);

    /// <summary>
    /// Downloads the text object at the given key. Returns null if the object does not exist.
    /// </summary>
    Task<string?> GetTextAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Deletes the object at the given key. No-ops if it does not exist.
    /// </summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Copies an object from <paramref name="sourceKey"/> to <paramref name="destKey"/> within the
    /// answers bucket.
    /// </summary>
    Task CopyAsync(string sourceKey, string destKey, CancellationToken ct = default);
}
