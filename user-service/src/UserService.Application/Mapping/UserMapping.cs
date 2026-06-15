using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Mapping;

/// <summary>Entity ↔ DTO dönüşümleri (harici mapper bağımlılığı olmadan).</summary>
internal static class UserMapping
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.FullName, user.Email, user.CreatedAt, user.UpdatedAt);
}
