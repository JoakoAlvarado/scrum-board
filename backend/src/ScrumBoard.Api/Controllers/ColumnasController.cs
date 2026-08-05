using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Services;

namespace ScrumBoard.Api.Controllers;

/// <summary>CRUD de columnas de un proyecto, incluido su orden (requisito 6.4).</summary>
[ApiController]
[Authorize]
[Route("api/proyectos/{proyectoId:guid}/columnas")]
public class ColumnasController : ControllerBase
{
    private readonly IColumnaService _columnaService;

    public ColumnasController(IColumnaService columnaService) => _columnaService = columnaService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ColumnaDto>>> Listar(Guid proyectoId, CancellationToken ct)
    {
        return Ok(await _columnaService.ListarAsync(proyectoId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ColumnaDto>> Crear(Guid proyectoId, CrearColumnaRequest request, CancellationToken ct)
    {
        var columna = await _columnaService.CrearAsync(proyectoId, request, ct);
        return CreatedAtAction(nameof(Listar), new { proyectoId }, columna);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ColumnaDto>> Actualizar(Guid proyectoId, Guid id, ActualizarColumnaRequest request, CancellationToken ct)
    {
        return Ok(await _columnaService.ActualizarAsync(proyectoId, id, request, ct));
    }

    /// <summary>Reordena la columna entre dos vecinas (drag & drop del tablero).</summary>
    [HttpPut("{id:guid}/orden")]
    public async Task<ActionResult<ColumnaDto>> Reordenar(Guid proyectoId, Guid id, ReordenarColumnaRequest request, CancellationToken ct)
    {
        return Ok(await _columnaService.ReordenarAsync(proyectoId, id, request, ct));
    }

    /// <summary>Falla con 409 (DomainException) si la columna todavía tiene tareas.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid proyectoId, Guid id, CancellationToken ct)
    {
        await _columnaService.EliminarAsync(proyectoId, id, ct);
        return NoContent();
    }
}
