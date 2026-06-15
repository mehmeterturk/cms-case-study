namespace ContentService.Application.Common;

/// <summary>Desteklenen içerik dilleri (appsettings: "Localization").</summary>
public class LocalizationOptions
{
    public const string SectionName = "Localization";

    /// <summary>
    /// İzin verilen ISO 639-1 dil kodları (küçük harf, ör. "tr", "en").
    /// Tek kaynak yapılandırmadır; varsayılan boştur (config binder'ın default'a
    /// ekleme yaparak çiftlemesini önlemek için).
    /// </summary>
    public string[] SupportedLanguages { get; set; } = [];
}
