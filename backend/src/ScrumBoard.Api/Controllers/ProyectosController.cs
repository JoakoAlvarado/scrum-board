using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Services;

namespace ScrumBoard.Api.Controllers;

/// <summary>
/// CRUD de proyectos (requisito 6.3). Las excepciones de negocio (no encontrado,
/// validación) las traduce a HTTP el ExceptionHandlingMiddleware — este controller
/// se limita a orquestar el caso de uso.
/// </summary>
[ApiController]
[Authorize]
[Route("api/proyectos")]
public class ProyectosController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public ProyectosController(IProyectoService proyectoService) => _proyectoService = proyectoService;

    /// <summary>Listado paginado con filtro opcional por nombre (coincidencia parcial, resuelto en el servidor).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProyectoDto>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanioPagina = 10,
        CancellationToken ct = default)
    {
        return Ok(await _proyectoService.ListarAsync(nombre, pagina, tamanioPagina, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProyectoDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        return Ok(await _proyectoService.ObtenerPorIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ProyectoDto>> Crear(CrearProyectoRequest request, CancellationToken ct)
    {
        var proyecto = await _proyectoService.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = proyecto.Id }, proyecto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProyectoDto>> Actualizar(Guid id, ActualizarProyectoRequest request, CancellationToken ct)
    {
        return Ok(await _proyectoService.ActualizarAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _proyectoService.EliminarAsync(id, ct);
        return NoContent();
    }
}
