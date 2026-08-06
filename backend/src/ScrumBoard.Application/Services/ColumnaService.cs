using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Domain.Services;

namespace ScrumBoard.Application.Services;

/// <summary>
/// Gestión de columnas. Todas las mutaciones pasan por el agregado <see cref="Proyecto"/>
/// (no hay un IColumnaRepository: Columna no es una raíz de agregado) — ver
/// docs/decisiones.md.
/// </summary>
public class ColumnaService : IColumnaService
{
    private readonly IProyectoRepository _proyectoRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ColumnaService(IProyectoRepository proyectoRepository, IRealtimeNotifier realtimeNotifier)
    {
        _proyectoRepository = proyectoRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<IReadOnlyList<ColumnaDto>> ListarAsync(Guid proyectoId, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        return proyecto.Columnas
            .OrderBy(c => c.Orden)
            .Select(c => MapearADto(c, proyecto))
            .ToList();
    }

    public async Task<ColumnaDto> CrearAsync(Guid proyectoId, CrearColumnaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        // Nueva columna siempre al final del tablero.
        var ultimaOrden = proyecto.Columnas.OrderBy(c => c.Orden).LastOrDefault()?.Orden;
        var nuevoOrden = CalculadorDeOrden.CalcularOrden(ultimaOrden, null);

        var columna = proyecto.AgregarColumna(request.Nombre, nuevoOrden);

        await _proyectoRepository.GuardarCambiosAsync(ct);

        var dto = MapearADto(columna, proyecto);
        await _realtimeNotifier.NotificarColumnaCreadaAsync(proyectoId, dto, ct);

        return dto;
    }

    public async Task<ColumnaDto> ActualizarAsync(Guid proyectoId, Guid columnaId, ActualizarColumnaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        proyecto.RenombrarColumna(columnaId, request.Nombre);
        await _proyectoRepository.GuardarCambiosAsync(ct);

        var columna = proyecto.Columnas.First(c => c.Id == columnaId);
        var dto = MapearADto(columna, proyecto);
        await _realtimeNotifier.NotificarColumnaActualizadaAsync(proyectoId, dto, ct);

        return dto;
    }

    public async Task<ColumnaDto> ReordenarAsync(Guid proyectoId, Guid columnaId, ReordenarColumnaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        var ordenAnterior = ObtenerOrdenColumna(proyecto, request.ColumnaAnteriorId);
        var ordenSiguiente = ObtenerOrdenColumna(proyecto, request.ColumnaSiguienteId);
        var nuevoOrden = CalculadorDeOrden.CalcularOrden(ordenAnterior, ordenSiguiente);

        proyecto.ReordenarColumna(columnaId, nuevoOrden);
        await _proyectoRepository.GuardarCambiosAsync(ct);

        var columna = proyecto.Columnas.First(c => c.Id == columnaId);
        var dto = MapearADto(columna, proyecto);
        await _realtimeNotifier.NotificarColumnaReordenadaAsync(proyectoId, dto, ct);

        return dto;
    }

    public async Task EliminarAsync(Guid proyectoId, Guid columnaId, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        proyecto.EliminarColumna(columnaId); // lanza DomainException si tiene tareas
        await _proyectoRepository.GuardarCambiosAsync(ct);

        await _realtimeNotifier.NotificarColumnaEliminadaAsync(proyectoId, columnaId, ct);
    }

    private async Task<Proyecto> ObtenerProyectoOFallarAsync(Guid proyectoId, CancellationToken ct) =>
        await _proyectoRepository.ObtenerConTableroAsync(proyectoId, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", proyectoId);

    private static decimal? ObtenerOrdenColumna(Proyecto proyecto, Guid? columnaId) =>
        columnaId is null ? null : proyecto.Columnas.FirstOrDefault(c => c.Id == columnaId)?.Orden;

    private static ColumnaDto MapearADto(Columna columna, Proyecto proyecto) => new(
        columna.Id,
        columna.Nombre,
        columna.Orden,
        columna.ProyectoId,
        proyecto.Tareas.Count(t => t.ColumnaId == columna.Id));
}
