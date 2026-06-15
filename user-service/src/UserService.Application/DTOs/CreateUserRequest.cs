namespace UserService.Application.DTOs;

/// <summary>Yeni kullanıcı oluşturma isteği.</summary>
public record CreateUserRequest(string FullName, string Email);
