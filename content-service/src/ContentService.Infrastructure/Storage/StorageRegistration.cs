using Amazon;
using Amazon.S3;
using Azure.Storage.Blobs;
using ContentService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContentService.Infrastructure.Storage;

/// <summary>
/// Yapılandırmadaki "Storage:Provider" değerine göre IFileStorage sokete uygun
/// sağlayıcıyı (Local / S3 / AzureBlob) bağlar. Yalnızca seçilen sağlayıcı
/// örneklendiği için diğerlerinin kimlik bilgileri/SDK ayarları gerekmez.
/// </summary>
public static class StorageRegistration
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        services.Configure<StorageOptions>(section);

        var options = section.Get<StorageOptions>() ?? new StorageOptions();

        switch (options.Provider.Trim().ToLowerInvariant())
        {
            case "s3":
                services.AddSingleton<IAmazonS3>(_ =>
                {
                    var config = new AmazonS3Config();
                    if (!string.IsNullOrWhiteSpace(options.S3.ServiceUrl))
                    {
                        config.ServiceURL = options.S3.ServiceUrl;
                        config.ForcePathStyle = true; // MinIO vb. için
                    }
                    else if (!string.IsNullOrWhiteSpace(options.S3.Region))
                    {
                        config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.S3.Region);
                    }

                    return new AmazonS3Client(config); // kimlik bilgileri varsayılan zincirden alınır
                });
                services.AddSingleton<IFileStorage>(sp => new S3FileStorage(
                    sp.GetRequiredService<IAmazonS3>(),
                    options.S3.BucketName,
                    sp.GetRequiredService<ILogger<S3FileStorage>>()));
                break;

            case "azureblob":
                services.AddSingleton<IFileStorage>(sp =>
                {
                    var container = new BlobContainerClient(
                        options.AzureBlob.ConnectionString,
                        options.AzureBlob.ContainerName);
                    container.CreateIfNotExists();
                    return new AzureBlobFileStorage(container, sp.GetRequiredService<ILogger<AzureBlobFileStorage>>());
                });
                break;

            default: // Local (varsayılan)
                services.AddSingleton<IFileStorage, LocalFileStorage>();
                break;
        }

        return services;
    }
}
