namespace ContentService.Application.DTOs;

/// <summary>Mevcut içeriği güncelleme isteği (sahip kullanıcı değiştirilmez).</summary>
public record UpdateContentRequest(string Title, string Body);
