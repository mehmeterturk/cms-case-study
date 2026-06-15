namespace UserService.Application.DTOs;

/// <summary>Mevcut kullanıcıyı güncelleme isteği.</summary>
public record UpdateUserRequest(string FullName, string Email);
