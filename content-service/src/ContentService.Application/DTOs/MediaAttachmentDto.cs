namespace ContentService.Application.DTOs;

/// <summary>Bir içeriğe ekli medya dosyasının üst verisi.</summary>
public record MediaAttachmentDto(
    Guid Id,
    Guid ContentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);
