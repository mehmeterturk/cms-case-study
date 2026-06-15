namespace ContentService.Application.DTOs;

/// <summary>API'nin dış dünyaya döndürdüğü içerik görünümü (ekli medyalar dahil).</summary>
public record ContentDto(
    Guid Id,
    string Title,
    string Body,
    string Slug,
    Guid UserId,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<MediaAttachmentDto> Media);
