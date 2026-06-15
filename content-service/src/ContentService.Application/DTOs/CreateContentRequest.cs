namespace ContentService.Application.DTOs;

/// <summary>
/// Yeni içerik oluşturma isteği. <see cref="UserId"/> User Service'te doğrulanır.
/// <see cref="Slug"/> verilmezse başlıktan otomatik üretilir.
/// <see cref="TranslationGroupId"/> verilirse içerik mevcut bir çeviri grubuna
/// (başka dildeki versiyonlara) eklenir; verilmezse yeni bir grup başlatılır.
/// </summary>
public record CreateContentRequest(
    string Title,
    string Body,
    Guid UserId,
    string Language,
    Guid? TranslationGroupId = null,
    string? Slug = null);
