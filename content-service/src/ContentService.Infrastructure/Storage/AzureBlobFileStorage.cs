using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ContentService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentService.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage için depolama sağlayıcısı. Yapılandırmada Provider="AzureBlob"
/// seçildiğinde devreye girer.
/// </summary>
public class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    public AzureBlobFileStorage(BlobContainerClient container, ILogger<AzureBlobFileStorage> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
        _logger.LogInformation("Dosya Azure Blob'a yazıldı: {Container}/{Key}", _container.Name, storageKey);
    }

    public async Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
