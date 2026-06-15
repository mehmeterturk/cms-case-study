using Amazon.S3;
using Amazon.S3.Model;
using ContentService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentService.Infrastructure.Storage;

/// <summary>
/// Amazon S3 (veya MinIO gibi S3-uyumlu servisler) için depolama sağlayıcısı.
/// Yapılandırmada Provider="S3" seçildiğinde devreye girer.
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<S3FileStorage> _logger;

    public S3FileStorage(IAmazonS3 s3, string bucket, ILogger<S3FileStorage> logger)
    {
        _s3 = s3;
        _bucket = bucket;
        _logger = logger;
    }

    public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Dosya S3'e yazıldı: {Bucket}/{Key}", _bucket, storageKey);
    }

    public async Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_bucket, storageKey, cancellationToken);
    }
}
