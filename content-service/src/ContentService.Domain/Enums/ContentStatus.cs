namespace ContentService.Domain.Enums;

/// <summary>Bir içeriğin yayın yaşam döngüsündeki durumu.</summary>
public enum ContentStatus
{
    /// <summary>Taslak — oluşturulmuş ama henüz yayınlanmamış.</summary>
    Draft = 0,

    /// <summary>Yayında — herkese görünür.</summary>
    Published = 1,

    /// <summary>Arşivlenmiş — yayından kaldırılmış.</summary>
    Archived = 2
}
