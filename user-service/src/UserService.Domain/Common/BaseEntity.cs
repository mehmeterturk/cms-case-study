namespace UserService.Domain.Common;

/// <summary>
/// Tüm entity'ler için ortak alanlar: kimlik ve denetim (audit) zaman damgaları.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
