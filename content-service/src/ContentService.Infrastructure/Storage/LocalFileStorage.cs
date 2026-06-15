using ContentService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentService.Infrastructure.Storage;

/// <summary>
/// Dosyaları yerel diske (konteynerde bir volume'a) yazan sağlayıcı. Bulut hesabı
/// gerektirmediği için geliştirme ve bu vaka için varsayılan sağlayıcıdır.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<StorageOptions> options, ILogger<LocalFileStorage> logger)
    {
        _rootPath = options.Value.Local.RootPath;
        _logger = logger;
        Directory.CreateDirectory(_rootPath);
    }

    public string ProviderName => "Local";

    public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        _logger.LogInformation("Dosya yerel depoya yazıldı: {Key}", storageKey);
    }

    public Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Depoda dosya bulunamadı: {storageKey}");
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>Anahtarı kök dizin altına güvenli biçimde çözer (path traversal'a karşı).</summary>
    private string ResolvePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var rootFull = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Geçersiz depolama anahtarı.");
        }

        return fullPath;
    }
}
