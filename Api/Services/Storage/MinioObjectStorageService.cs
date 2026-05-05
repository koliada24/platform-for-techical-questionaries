using System.Text;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Api.Services.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool UseSSL { get; set; } = false;
    public string Bucket { get; set; } = "qapp-answers";
}

public class MinioObjectStorageService : IObjectStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucket;
    private readonly ILogger<MinioObjectStorageService> _logger;

    public MinioObjectStorageService(
        IMinioClient client,
        IOptions<MinioOptions> options,
        ILogger<MinioObjectStorageService> logger)
    {
        _client = client;
        _bucket = options.Value.Bucket;
        _logger = logger;
    }

    public async Task PutTextAsync(string key, string content, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        _logger.LogInformation("MinIO PUT {Bucket}/{Key} ({Bytes} bytes)", _bucket, key, bytes.Length);
        try
        {
            using var ms = new MemoryStream(bytes);
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithStreamData(ms)
                .WithObjectSize(bytes.LongLength)
                .WithContentType("text/plain; charset=utf-8"), ct);
            _logger.LogInformation("MinIO PUT {Bucket}/{Key} succeeded", _bucket, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO PUT {Bucket}/{Key} failed", _bucket, key);
            throw;
        }
    }

    public async Task<string?> GetTextAsync(string key, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        try
        {
            await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithCallbackStream(async (stream, cancel) =>
                {
                    await stream.CopyToAsync(ms, cancel);
                }), ct);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key), ct);
        }
        catch (ObjectNotFoundException)
        {
            // Idempotent.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete object {Key} from bucket {Bucket}.", key, _bucket);
        }
    }

    public async Task CopyAsync(string sourceKey, string destKey, CancellationToken ct = default)
    {
        _logger.LogInformation("MinIO COPY {Bucket}/{Src} -> {Bucket}/{Dst}", _bucket, sourceKey, destKey);
        var source = new CopySourceObjectArgs()
            .WithBucket(_bucket)
            .WithObject(sourceKey);

        await _client.CopyObjectAsync(new CopyObjectArgs()
            .WithBucket(_bucket)
            .WithObject(destKey)
            .WithCopyObjectSource(source), ct);
        _logger.LogInformation("MinIO COPY {Src} -> {Dst} succeeded", sourceKey, destKey);
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs()
            .WithBucket(_bucket), ct);
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs()
                .WithBucket(_bucket), ct);
        }
    }
}
