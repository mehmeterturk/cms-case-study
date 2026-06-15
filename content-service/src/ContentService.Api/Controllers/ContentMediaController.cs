using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Api.Controllers;

[ApiController]
[Route("contents/{contentId:guid}/media")]
[Produces("application/json")]
public class ContentMediaController : ControllerBase
{
    private readonly IMediaService _service;

    public ContentMediaController(IMediaService service)
    {
        _service = service;
    }

    /// <summary>Bir içeriğe ekli medya dosyalarını listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MediaAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MediaAttachmentDto>>> GetAll(Guid contentId, CancellationToken cancellationToken)
        => Ok(await _service.GetByContentAsync(contentId, cancellationToken));

    /// <summary>İçeriğe bir medya dosyası yükler (multipart/form-data, alan adı: file).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MediaAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAttachmentDto>> Upload(Guid contentId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest("Dosya (file) zorunludur.");
        }

        await using var stream = file.OpenReadStream();
        var upload = new FileUpload(stream, file.FileName, file.ContentType, file.Length);
        var created = await _service.UploadAsync(contentId, upload, cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { contentId }, created);
    }

    /// <summary>Bir medya dosyasını indirir.</summary>
    [HttpGet("{mediaId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid contentId, Guid mediaId, CancellationToken cancellationToken)
    {
        var file = await _service.DownloadAsync(contentId, mediaId, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Bir medya dosyasını siler (depodan ve veritabanından).</summary>
    [HttpDelete("{mediaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid contentId, Guid mediaId, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(contentId, mediaId, cancellationToken);
        return NoContent();
    }
}
