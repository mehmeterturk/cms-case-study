using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Kullanıcı veri erişim sözleşmesi. Implementasyonu Infrastructure katmanındadır,
/// böylece Application katmanı EF Core'a bağımlı olmaz.
/// </summary>
public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
