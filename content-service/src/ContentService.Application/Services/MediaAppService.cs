using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Models;
using ContentService.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace ContentService.Application.Services;

/// <summary>
/// İçeriğe medya ekleme iş mantığı. Dosyanın baytları depolama soketine
/// (<see cref="IFileStorage"/>) yazılır; üst veri veritabanında tutulur.
/// </summary>
public class MediaAppService : IMediaService
{
    /// <summary>İzin verilen en büyük dosya boyutu (25 MB).</summary>
    private const long MaxFileSizeBytes = 25L * 1024 * 1024;

    private readonly IContentRepository _contentRepository;
    private readonly IMediaAttachmentRepository _mediaRepository;
    private readonly IFileStorage _storage;

    public MediaAppService(
        IContentRepository contentRepository,
        IMediaAttachmentRepository mediaRepository,
        IFileStorage storage)
    {
        _contentRepository = contentRepository;
        _mediaRepository = mediaRepository;
        _storage = storage;
    }

    public async Task<IReadOnlyList<MediaAttachmentDto>> GetByContentAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        await EnsureContentExistsAsync(contentId, cancellationToken);
        var items = await _mediaRepository.GetByContentIdAsync(contentId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<MediaAttachmentDto> UploadAsync(Guid contentId, FileUpload file, CancellationToken cancellationToken = default)
    {
        await EnsureContentExistsAsync(contentId, cancellationToken);
        Validate(file);

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
        return ToDto(media);
    }

    public async Task<FileDownload> DownloadAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await GetOwnedMediaAsync(contentId, mediaId, cancellationToken);
        var stream = await _storage.GetAsync(media.StorageKey, cancellationToken);
        return new FileDownload(stream, media.FileName, media.ContentType);
    }

    public async Task DeleteAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await GetOwnedMediaAsync(contentId, mediaId, cancellationToken);
        await _storage.DeleteAsync(media.StorageKey, cancellationToken);
        await _mediaRepository.DeleteAsync(media, cancellationToken);
    }

    private async Task EnsureContentExistsAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var content = await _contentRepository.GetByIdAsync(contentId, cancellationToken);
        if (content is null)
        {
            throw new NotFoundException("İçerik", contentId);
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

    private static void Validate(FileUpload file)
    {
        var failures = new List<ValidationFailure>();

        if (file.SizeBytes <= 0)
        {
            failures.Add(new ValidationFailure(nameof(file.Content), "Boş dosya yüklenemez."));
        }

        if (file.SizeBytes > MaxFileSizeBytes)
        {
            failures.Add(new ValidationFailure(nameof(file.SizeBytes), "Dosya boyutu 25 MB sınırını aşıyor."));
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            failures.Add(new ValidationFailure(nameof(file.FileName), "Dosya adı zorunludur."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    private static MediaAttachmentDto ToDto(MediaAttachment m) =>
        new(m.Id, m.ContentId, m.FileName, m.ContentType, m.SizeBytes, m.CreatedAt);
}
