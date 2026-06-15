using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Mapping;
using ContentService.Domain.Entities;
using FluentValidation;

namespace ContentService.Application.Services;

/// <summary>
/// İçerik iş mantığı. İçerik oluştururken, sahip kullanıcının User Service'te
/// var olduğunu servisler arası çağrı ile doğrular (veri tutarlılığı).
/// </summary>
public class ContentAppService : IContentService
{
    private readonly IContentRepository _repository;
    private readonly IUserValidationClient _userClient;
    private readonly IValidator<CreateContentRequest> _createValidator;
    private readonly IValidator<UpdateContentRequest> _updateValidator;

    public ContentAppService(
        IContentRepository repository,
        IUserValidationClient userClient,
        IValidator<CreateContentRequest> createValidator,
        IValidator<UpdateContentRequest> updateValidator)
    {
        _repository = repository;
        _userClient = userClient;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ContentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contents = await _repository.GetAllAsync(cancellationToken);
        return contents.Select(c => c.ToDto()).ToList();
    }

    public async Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);
        return content.ToDto();
    }

    public async Task<ContentDto> CreateAsync(CreateContentRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Servisler arası doğrulama: kullanıcı yoksa içerik oluşturulmaz.
        var userExists = await _userClient.UserExistsAsync(request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new ValidationException(
                $"Belirtilen kullanıcı ({request.UserId}) bulunamadı; içerik oluşturulamaz.");
        }

        var content = new Content
        {
            Title = request.Title,
            Body = request.Body,
            UserId = request.UserId
        };

        await _repository.AddAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        content.Title = request.Title;
        content.Body = request.Body;
        content.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        await _repository.DeleteAsync(content, cancellationToken);
    }
}
