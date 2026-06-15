namespace ContentService.Application.DTOs;

/// <summary>API'nin dış dünyaya döndürdüğü içerik görünümü.</summary>
public record ContentDto(
    Guid Id,
    string Title,
    string Body,
    Guid UserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
