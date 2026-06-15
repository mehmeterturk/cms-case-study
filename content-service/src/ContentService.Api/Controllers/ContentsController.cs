using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
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

    /// <summary>Tüm içerikleri listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    /// <summary>Belirli bir içeriği getirir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Yeni içerik oluşturur. Sahip kullanıcı User Service'te doğrulanır;
    /// kullanıcı yoksa 400, doğrulama servisine erişilemezse 502 döner.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ContentDto>> Create([FromBody] CreateContentRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Belirli bir içeriği günceller.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Update(Guid id, [FromBody] UpdateContentRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Belirli bir içeriği siler.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
