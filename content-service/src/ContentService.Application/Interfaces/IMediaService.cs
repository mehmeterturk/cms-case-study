using ContentService.Application.DTOs;
using ContentService.Application.Models;

namespace ContentService.Application.Interfaces;

public interface IMediaService
{
    Task<IReadOnlyList<MediaAttachmentDto>> GetByContentAsync(Guid contentId, CancellationToken cancellationToken = default);

    Task<MediaAttachmentDto> UploadAsync(Guid contentId, FileUpload file, CancellationToken cancellationToken = default);

    Task<FileDownload> DownloadAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid contentId, Guid mediaId, CancellationToken cancellationToken = default);
}
