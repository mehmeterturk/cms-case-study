using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Models;
using ContentService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Api.Controllers;

[ApiController]
[Route("contents")]
[Produces("application/json")]
public class ContentsController : ControllerBase
{
    private readonly IContentService _service;

    public ContentsController(IContentService service)
    {
        _service = service;
    }

    /// <summary>İçerikleri (ekli medyalarıyla) listeler. Opsiyonel: ?status=Published.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentDto>>> GetAll([FromQuery] ContentStatus? status, CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(status, cancellationToken));

    /// <summary>Belirli bir içeriği ekli medyalarıyla getirir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Slug'a göre içeriği ekli medyalarıyla getirir.</summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetBySlug(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetBySlugAsync(slug, cancellationToken));

    /// <summary>
    /// Yeni içerik oluşturur (taslak) ve isteğe bağlı medya dosyaları ekler.
    /// multipart/form-data: title, body, userId, (opsiyonel) slug, (opsiyonel) files.
    /// Sahip kullanıcı User Service'te doğrulanır; yoksa 400, servise erişilemezse 502.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ContentDto>> Create(
        [FromForm] string title,
        [FromForm] string body,
        [FromForm] Guid userId,
        [FromForm] string? slug,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var request = new CreateContentRequest(title, body, userId, slug);
        var uploads = ToUploads(files);
        var created = await _service.CreateAsync(request, uploads, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// İçeriğin başlık/gövdesini günceller ve isteğe bağlı yeni medya dosyaları ekler
    /// (mevcut medyalar korunur). multipart/form-data: title, body, (opsiyonel) files.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Update(
        Guid id,
        [FromForm] string title,
        [FromForm] string body,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var request = new UpdateContentRequest(title, body);
        var uploads = ToUploads(files);
        return Ok(await _service.UpdateAsync(id, request, uploads, cancellationToken));
    }

    /// <summary>İçeriği yayına alır. Zaten yayındaysa 409 döner.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentDto>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.PublishAsync(id, cancellationToken));

    /// <summary>İçeriği arşivler. Zaten arşivlenmişse 409 döner.</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentDto>> Archive(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.ArchiveAsync(id, cancellationToken));

    /// <summary>Belirli bir içeriği siler (ekli medya dosyaları da temizlenir).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>İçeriğe ait bir medya dosyasını indirir.</summary>
    [HttpGet("{id:guid}/media/{mediaId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMedia(Guid id, Guid mediaId, CancellationToken cancellationToken)
    {
        var file = await _service.DownloadMediaAsync(id, mediaId, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>İçeriğe ait bir medya dosyasını siler (depodan + veritabanından).</summary>
    [HttpDelete("{id:guid}/media/{mediaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedia(Guid id, Guid mediaId, CancellationToken cancellationToken)
    {
        await _service.DeleteMediaAsync(id, mediaId, cancellationToken);
        return NoContent();
    }

    private static IReadOnlyList<FileUpload> ToUploads(List<IFormFile>? files) =>
        (files ?? [])
            .Select(f => new FileUpload(f.OpenReadStream(), f.FileName, f.ContentType, f.Length))
            .ToList();
}
