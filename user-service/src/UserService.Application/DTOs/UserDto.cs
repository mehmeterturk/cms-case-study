namespace UserService.Application.DTOs;

/// <summary>API'nin dış dünyaya döndürdüğü kullanıcı görünümü.</summary>
public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
