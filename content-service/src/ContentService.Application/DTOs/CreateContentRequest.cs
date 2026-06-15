namespace ContentService.Application.DTOs;

/// <summary>
/// Yeni içerik oluşturma isteği. <see cref="UserId"/> User Service'te doğrulanır.
/// <see cref="Slug"/> verilmezse başlıktan otomatik üretilir.
/// </summary>
public record CreateContentRequest(string Title, string Body, Guid UserId, string? Slug = null);
