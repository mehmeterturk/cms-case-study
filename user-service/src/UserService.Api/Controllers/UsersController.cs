using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    /// <summary>Tüm kullanıcıları listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    /// <summary>Belirli bir kullanıcıyı getirir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Kullanıcı oluşturur. Gövde tek bir nesne ise tek kullanıcı, bir dizi ise
    /// birden çok kullanıcı (atomik) oluşturulur.
    /// Tekil → tek kullanıcı (201), dizi → kullanıcı listesi (201) döner.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (body.ValueKind == JsonValueKind.Array)
        {
            List<CreateUserRequest>? requests;
            try
            {
                requests = body.Deserialize<List<CreateUserRequest>>(JsonOptions);
            }
            catch (JsonException)
            {
                return BadRequest("Geçersiz kullanıcı listesi gövdesi.");
            }

            var createdMany = await _service.CreateManyAsync(requests ?? [], cancellationToken);
            return StatusCode(StatusCodes.Status201Created, createdMany);
        }

        if (body.ValueKind == JsonValueKind.Object)
        {
            CreateUserRequest? request;
            try
            {
                request = body.Deserialize<CreateUserRequest>(JsonOptions);
            }
            catch (JsonException)
            {
                return BadRequest("Geçersiz kullanıcı gövdesi.");
            }

            var created = await _service.CreateAsync(request!, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        return BadRequest("Gövde bir kullanıcı nesnesi veya kullanıcı dizisi olmalıdır.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Belirli bir kullanıcıyı günceller.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Belirli bir kullanıcıyı siler.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
