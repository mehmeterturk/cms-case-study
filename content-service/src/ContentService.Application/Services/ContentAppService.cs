using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Mapping;
using ContentService.Application.Models;
using ContentService.Domain.Common;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace ContentService.Application.Services;

/// <summary>
/// İçerik iş mantığı. İçerik oluştururken sahip kullanıcının User Service'te var
/// olduğunu doğrular (veri tutarlılığı), tekil bir slug üretir, yayın yaşam
/// döngüsünü (taslak → yayın → arşiv) yönetir ve ekli medya dosyalarını
/// (içerik aggregate'inin parçası) yönetir.
/// </summary>
public class ContentAppService : IContentService
{
    /// <summary>İzin verilen en büyük dosya boyutu (25 MB).</summary>
    private const long MaxFileSizeBytes = 25L * 1024 * 1024;

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

    public async Task<IReadOnlyList<ContentDto>> GetAllAsync(ContentStatus? status = null, string? language = null, CancellationToken cancellationToken = default)
    {
        var contents = await _repository.GetAllAsync(status, language, cancellationToken);
        return contents.Select(c => c.ToDto()).ToList();
    }

    public async Task<ContentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);
        return content.ToDto();
    }

    public async Task<IReadOnlyList<ContentDto>> GetTranslationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        var siblings = await _repository.GetByTranslationGroupAsync(content.TranslationGroupId, cancellationToken);
        return siblings.Select(c => c.ToDto()).ToList();
    }

    public async Task<ContentDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var content = await _repository.GetBySlugAsync(slug, cancellationToken)
                      ?? throw new NotFoundException($"'{slug}' slug'ına sahip içerik bulunamadı.");
        return content.ToDto();
    }

    public async Task<ContentDto> CreateAsync(CreateContentRequest request, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        ValidateFiles(files);

        // Servisler arası doğrulama: kullanıcı yoksa içerik oluşturulmaz.
        var userExists = await _userClient.UserExistsAsync(request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new ValidationException(
                $"Belirtilen kullanıcı ({request.UserId}) bulunamadı; içerik oluşturulamaz.");
        }

        var translationGroupId = await ResolveTranslationGroupAsync(request, cancellationToken);

        var baseSlug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug);
        var slug = await EnsureUniqueSlugAsync(baseSlug, cancellationToken);

        var content = new Content
        {
            Title = request.Title,
            Body = request.Body,
            Slug = slug,
            Language = request.Language,
            TranslationGroupId = translationGroupId,
            UserId = request.UserId,
            Status = ContentStatus.Draft
        };

        await _repository.AddAsync(content, cancellationToken);

        await AttachFilesAsync(content.Id, files, cancellationToken);
        content.MediaAttachments = (await _mediaRepository.GetByContentIdAsync(content.Id, cancellationToken)).ToList();
        return content.ToDto();
    }

    public async Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        ValidateFiles(files);

        var content = await _repository.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("İçerik", id);

        content.UpdateDetails(request.Title, request.Body);
        await _repository.UpdateAsync(content, cancellationToken);

        // Replace mantığı: mevcut medyalar (depo + kayıt) temizlenir, gönderilen
        // dosyalar yeni medya seti olur. Dosya gönderilmezse medya tamamen kaldırılır.
        await ClearMediaAsync(content, cancellationToken);
        await AttachFilesAsync(id, files, cancellationToken);

        content.MediaAttachments = (await _mediaRepository.GetByContentIdAsync(id, cancellationToken)).ToList();
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
        foreach (var item in content.MediaAttachments)
        {
            await _storage.DeleteAsync(item.StorageKey, cancellationToken);
        }

        await _repository.DeleteAsync(content, cancellationToken);
    }

    public async Task<FileDownload> DownloadMediaAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await GetOwnedMediaAsync(contentId, mediaId, cancellationToken);
        var stream = await _storage.GetAsync(media.StorageKey, cancellationToken);
        return new FileDownload(stream, media.FileName, media.ContentType);
    }

    /// <summary>
    /// İçeriğe ait tüm medyaları depodan ve veritabanından temizler. Zaten yüklü
    /// (tracked) navigation üzerinden çalışır; böylece EF tracking çakışması olmaz.
    /// </summary>
    private async Task ClearMediaAsync(Content content, CancellationToken cancellationToken)
    {
        foreach (var media in content.MediaAttachments.ToList())
        {
            await _storage.DeleteAsync(media.StorageKey, cancellationToken);
            await _mediaRepository.DeleteAsync(media, cancellationToken);
        }
    }

    private async Task AttachFilesAsync(Guid contentId, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            var storageKey = $"{contentId}/{Guid.NewGuid():N}{extension}";

            await _storage.SaveAsync(storageKey, file.Content, file.ContentType, cancellationToken);

            var media = new MediaAttachment
            {
                ContentId = contentId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                StorageKey = storageKey,
                SizeBytes = file.SizeBytes
            };

            await _mediaRepository.AddAsync(media, cancellationToken);
        }
    }

    private async Task<MediaAttachment> GetOwnedMediaAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media is null || media.ContentId != contentId)
        {
            throw new NotFoundException($"İçeriğe ({contentId}) ait medya ({mediaId}) bulunamadı.");
        }

        return media;
    }

    /// <summary>
    /// Çeviri grubunu çözer: grup verilmezse yeni bir grup başlatır; verilirse grubun
    /// var olduğunu ve o grupta aynı dilin henüz bulunmadığını doğrular.
    /// </summary>
    private async Task<Guid> ResolveTranslationGroupAsync(CreateContentRequest request, CancellationToken cancellationToken)
    {
        if (request.TranslationGroupId is not Guid groupId)
        {
            return Guid.NewGuid(); // ilk versiyon: yeni grup
        }

        if (!await _repository.TranslationGroupExistsAsync(groupId, cancellationToken))
        {
            throw new ValidationException($"Belirtilen çeviri grubu ({groupId}) bulunamadı.");
        }

        if (await _repository.ExistsInGroupWithLanguageAsync(groupId, request.Language, cancellationToken))
        {
            throw new ValidationException($"Bu çeviri grubunda '{request.Language}' dili zaten mevcut.");
        }

        return groupId;
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

    private static void ValidateFiles(IReadOnlyList<FileUpload> files)
    {
        var failures = new List<ValidationFailure>();

        foreach (var file in files)
        {
            if (file.SizeBytes <= 0)
            {
                failures.Add(new ValidationFailure("file", $"Boş dosya yüklenemez: {file.FileName}"));
            }

            if (file.SizeBytes > MaxFileSizeBytes)
            {
                failures.Add(new ValidationFailure("file", $"Dosya 25 MB sınırını aşıyor: {file.FileName}"));
            }

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                failures.Add(new ValidationFailure("file", "Dosya adı zorunludur."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
