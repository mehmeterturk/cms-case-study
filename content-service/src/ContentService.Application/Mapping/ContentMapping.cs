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
            content.UpdatedAt);
}
