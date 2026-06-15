using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

/// <summary>Kullanıcı iş mantığı sözleşmesi.</summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Birden çok kullanıcıyı tek seferde (atomik) oluşturur.</summary>
    Task<IReadOnlyList<UserDto>> CreateManyAsync(IReadOnlyList<CreateUserRequest> requests, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
