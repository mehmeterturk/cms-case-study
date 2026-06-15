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

    public async Task<IReadOnlyList<UserDto>> CreateManyAsync(IReadOnlyList<CreateUserRequest> requests, CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            throw new ValidationException("En az bir kullanıcı göndermelisiniz.");
        }

        var failures = new List<string>();

        // 1) Her kaydı tek tek doğrula.
        for (var i = 0; i < requests.Count; i++)
        {
            var result = await _createValidator.ValidateAsync(requests[i], cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors.Select(e => $"[{i}] {e.ErrorMessage}"));
            }
        }

        // 2) Batch içi tekrar eden e-postalar.
        var duplicatesInBatch = requests
            .GroupBy(r => r.Email)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var email in duplicatesInBatch)
        {
            failures.Add($"'{email}' e-postası istekte birden fazla geçiyor.");
        }

        // 3) Veritabanında zaten var olan e-postalar.
        var existing = await _repository.GetExistingEmailsAsync(requests.Select(r => r.Email), cancellationToken);
        foreach (var email in existing)
        {
            failures.Add($"'{email}' e-postası zaten kullanımda.");
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(string.Join(" ", failures));
        }

        var users = requests
            .Select(r => new User { FullName = r.FullName, Email = r.Email })
            .ToList();

        await _repository.AddRangeAsync(users, cancellationToken); // atomik
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        // E-posta değişiyorsa, başka bir kullanıcıda kullanımda olmadığını doğrula.
        if (request.Email != user.Email && await _repository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ValidationException($"'{request.Email}' e-postası zaten kullanımda.");
        }

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
