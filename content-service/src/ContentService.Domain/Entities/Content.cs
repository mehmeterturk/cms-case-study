using ContentService.Domain.Common;

namespace ContentService.Domain.Entities;

/// <summary>
/// Bir içerik kaydı. <see cref="UserId"/> ile User Service'teki bir kullanıcıya bağlanır.
/// </summary>
public class Content : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>İçeriğin sahibi olan kullanıcının kimliği (User Service'te doğrulanır).</summary>
    public Guid UserId { get; set; }
}
