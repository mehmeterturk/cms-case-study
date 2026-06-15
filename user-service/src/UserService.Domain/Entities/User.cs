using UserService.Domain.Common;

namespace UserService.Domain.Entities;

/// <summary>
/// Sistemdeki bir kullanıcıyı temsil eder. İçerikler bu kullanıcılara bağlanır.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
