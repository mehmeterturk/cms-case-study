namespace ContentService.Application.DTOs;

/// <summary>Yeni içerik oluşturma isteği. <see cref="UserId"/> User Service'te doğrulanır.</summary>
public record CreateContentRequest(string Title, string Body, Guid UserId);
