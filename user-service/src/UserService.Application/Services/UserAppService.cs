using FluentValidation;
using UserService.Application.Common.Exceptions;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Mapping;
using UserService.Domain.Entities;

namespace UserService.Application.Services;

/// <summary>
/// Kullanıcı iş mantığı: validasyon, tekillik kontrolü ve kalıcılık orkestrasyonu.
/// </summary>
public class UserAppService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserAppService(
        IUserRepository repository,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);
        return user.ToDto();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await _repository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ValidationException(
                $"'{request.Email}' e-postası zaten kullanımda.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email
        };

        await _repository.AddAsync(user, cancellationToken);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, cancellationToken);
        return user.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        await _repository.DeleteAsync(user, cancellationToken);
    }
}
