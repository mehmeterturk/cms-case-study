namespace ContentService.Infrastructure.Storage;

/// <summary>Depolama sağlayıcısı ve sağlayıcıya özel ayarlar (appsettings: "Storage").</summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Local | S3 | AzureBlob</summary>
    public string Provider { get; set; } = "Local";

    public LocalStorageOptions Local { get; set; } = new();

    public S3StorageOptions S3 { get; set; } = new();

    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public class LocalStorageOptions
{
    /// <summary>Dosyaların yazılacağı kök dizin (konteynerde bir volume'a bağlanır).</summary>
    public string RootPath { get; set; } = "/app/media";
}

public class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    /// <summary>MinIO gibi S3-uyumlu servisler için opsiyonel uç nokta.</summary>
    public string? ServiceUrl { get; set; }
}

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}
