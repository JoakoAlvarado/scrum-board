using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Services;
using ScrumBoard.Domain.Entities.Enums;

namespace ScrumBoard.Api.Controllers;

/// <summary>CRUD de tareas del tablero (requisito 6.5) + movimiento/reordenamiento (6.6).</summary>
[ApiController]
[Authorize]
[Route("api/proyectos/{proyectoId:guid}/tareas")]
public class TareasController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareasController(ITareaService tareaService) => _tareaService = tareaService;

    /// <summary>Filtros opcionales por columna, responsable y prioridad (requisito deseable 7).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TareaDto>>> Listar(
        Guid proyectoId,
        [FromQuery] Guid? columnaId,
        [FromQuery] Guid? responsableId,
        [FromQuery] Prioridad? prioridad,
        CancellationToken ct)
    {
        return Ok(await _tareaService.ListarAsync(proyectoId, columnaId, responsableId, prioridad, ct));
    }

    [HttpPost]
    public async Task<ActionResult<TareaDto>> Crear(Guid proyectoId, CrearTareaRequest request, CancellationToken ct)
    {
        var tarea = await _tareaService.CrearAsync(proyectoId, request, ct);
        return CreatedAtAction(nameof(Listar), new { proyectoId }, tarea);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TareaDto>> Actualizar(Guid proyectoId, Guid id, ActualizarTareaRequest request, CancellationToken ct)
    {
        return Ok(await _tareaService.ActualizarAsync(proyectoId, id, request, ct));
    }

    /// <summary>Traslado entre columnas y/o reordenamiento dentro de la misma (drag & drop del tablero).</summary>
    [HttpPut("{id:guid}/mover")]
    public async Task<ActionResult<TareaDto>> Mover(Guid proyectoId, Guid id, MoverTareaRequest request, CancellationToken ct)
    {
        return Ok(await _tareaService.MoverAsync(proyectoId, id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid proyectoId, Guid id, CancellationToken ct)
    {
        await _tareaService.EliminarAsync(proyectoId, id, ct);
        return NoContent();
    }
}
