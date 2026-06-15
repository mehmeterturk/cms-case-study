namespace ContentService.Application.Models;

/// <summary>Yüklenen bir dosyayı temsil eder (API'nin IFormFile'ından bağımsız).</summary>
public record FileUpload(Stream Content, string FileName, string ContentType, long SizeBytes);

/// <summary>İndirme için dosya akışı ve üst verisi.</summary>
public record FileDownload(Stream Content, string FileName, string ContentType);
