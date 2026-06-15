using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Mapping;
using ContentService.Domain.Common;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using FluentValidation;

namespace ContentService.Application.Services;

/// <summary>
/// İçerik iş mantığı. İçerik oluştururken sahip kullanıcının User Service'te var
/// olduğunu doğrular (veri tutarlılığı), tekil bir slug üretir ve yayın yaşam
/// döngüsünü (taslak → yayın → arşiv) yönetir.
/// </summary>
public class ContentAppService : IContentService
{
    private readonly IContentRepository _repository;
    private readonly IUserValidationClient _userClient;
    private readonly IMediaAttachmentRepository _mediaRepository;
    private readonly IFileStorage _storage;
    private readonly IValidator<CreateContentRequest> _createValidator;
    private readonly IValidator<UpdateContentRequest> _updateValidator;

    public ContentAppService(
        IContentRepository repository,
        IUserValidationClient userClient,
        IMediaAttachmentRepository mediaRepository,
        IFileStorage storage,
        IValidator<CreateContentRequest> createValidator,
        IValidator<UpdateContentRequest> updateValidator)
    {
        _repository = repository;
        _userClient = userClient;
        _mediaRepository = mediaRepository;
        _storage = storage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ContentDto>> GetAllAsync(ContentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var contents = await _repository.GetAllAsync(status, cancellationToken);
        return contents.Select(c => c.ToDto()).ToList();
    }

    public async Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);
        return content.ToDto();
    }

    public async Task<ContentDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetBySlugAsync(slug, cancellationToken)
                      ?? throw new NotFoundException($"'{slug}' slug'ına sahip içerik bulunamadı.");
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

        var baseSlug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug);
        var slug = await EnsureUniqueSlugAsync(baseSlug, cancellationToken);

        var content = new Content
        {
            Title = request.Title,
            Body = request.Body,
            Slug = slug,
            UserId = request.UserId,
            Status = ContentStatus.Draft
        };

        await _repository.AddAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        content.UpdateDetails(request.Title, request.Body);

        await _repository.UpdateAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task<ContentDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        content.Publish();

        await _repository.UpdateAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task<ContentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        content.Archive();

        await _repository.UpdateAsync(content, cancellationToken);
        return content.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        // İçeriğe ait medya dosyalarını depodan temizle (DB kayıtları cascade ile silinir).
        var media = await _mediaRepository.GetByContentIdAsync(id, cancellationToken);
        foreach (var item in media)
        {
            await _storage.DeleteAsync(item.StorageKey, cancellationToken);
        }

        await _repository.DeleteAsync(content, cancellationToken);
    }

    /// <summary>
    /// Verilen taban slug çakışırsa sonuna artan bir sayı ekleyerek tekil hale getirir
    /// (ör. "merhaba-dunya", "merhaba-dunya-2", ...).
    /// </summary>
    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "icerik";
        }

        var candidate = baseSlug;
        var suffix = 2;
        while (await _repository.ExistsBySlugAsync(candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
