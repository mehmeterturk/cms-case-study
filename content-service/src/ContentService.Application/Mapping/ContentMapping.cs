using ContentService.Application.DTOs;
using ContentService.Domain.Entities;

namespace ContentService.Application.Mapping;

internal static class ContentMapping
{
    public static ContentDto ToDto(this Content content) =>
        new(
            content.Id,
            content.Title,
            content.Body,
            content.Slug,
            content.UserId,
            content.Status.ToString(),
            content.PublishedAt,
            content.CreatedAt,
            content.UpdatedAt,
            content.MediaAttachments
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.ToDto())
                .ToList());

    public static MediaAttachmentDto ToDto(this MediaAttachment media) =>
        new(media.Id, media.ContentId, media.FileName, media.ContentType, media.SizeBytes, media.CreatedAt);
}
