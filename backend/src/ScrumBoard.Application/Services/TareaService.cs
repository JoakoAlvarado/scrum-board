using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Domain.Entities.Enums;
using ScrumBoard.Domain.Services;

namespace ScrumBoard.Application.Services;

/// <summary>
/// Gestión de tareas. Igual que Columna, todas las mutaciones pasan por el agregado
/// <see cref="Proyecto"/> — ver docs/decisiones.md.
/// </summary>
public class TareaService : ITareaService
{
    private readonly IProyectoRepository _proyectoRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public TareaService(IProyectoRepository proyectoRepository, IUsuarioRepository usuarioRepository)
    {
        _proyectoRepository = proyectoRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IReadOnlyList<TareaDto>> ListarAsync(
        Guid proyectoId, Guid? columnaId, Guid? responsableId, Prioridad? prioridad, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        var tareas = proyecto.Tareas.AsEnumerable();

        if (columnaId is not null) tareas = tareas.Where(t => t.ColumnaId == columnaId);
        if (responsableId is not null) tareas = tareas.Where(t => t.ResponsableId == responsableId);
        if (prioridad is not null) tareas = tareas.Where(t => t.Prioridad == prioridad);

        return tareas.OrderBy(t => t.ColumnaId).ThenBy(t => t.Orden).Select(MapearADto).ToList();
    }

    public async Task<TareaDto> CrearAsync(Guid proyectoId, CrearTareaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);
        await ValidarResponsableExisteAsync(request.ResponsableId, ct);

        // Nueva tarea siempre al final de su columna.
        var ultimaOrden = proyecto.Tareas
            .Where(t => t.ColumnaId == request.ColumnaId)
            .OrderBy(t => t.Orden)
            .LastOrDefault()?.Orden;
        var nuevoOrden = CalculadorDeOrden.CalcularOrden(ultimaOrden, null);

        var tarea = proyecto.AgregarTarea(
            request.ColumnaId, request.Titulo, request.Descripcion, request.Prioridad, request.ResponsableId, nuevoOrden);

        await _proyectoRepository.GuardarCambiosAsync(ct);

        return MapearADto(tarea);
    }

    public async Task<TareaDto> ActualizarAsync(Guid proyectoId, Guid tareaId, ActualizarTareaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);
        await ValidarResponsableExisteAsync(request.ResponsableId, ct);

        proyecto.EditarTarea(tareaId, request.Titulo, request.Descripcion, request.Prioridad, request.ResponsableId);
        await _proyectoRepository.GuardarCambiosAsync(ct);

        var tarea = proyecto.Tareas.First(t => t.Id == tareaId);
        return MapearADto(tarea);
    }

    public async Task<TareaDto> MoverAsync(Guid proyectoId, Guid tareaId, MoverTareaRequest request, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        var ordenAnterior = ObtenerOrdenTareaEnColumnaDestino(proyecto, request.ColumnaDestinoId, request.TareaAnteriorId);
        var ordenSiguiente = ObtenerOrdenTareaEnColumnaDestino(proyecto, request.ColumnaDestinoId, request.TareaSiguienteId);
        var nuevoOrden = CalculadorDeOrden.CalcularOrden(ordenAnterior, ordenSiguiente);

        proyecto.MoverTarea(tareaId, request.ColumnaDestinoId, nuevoOrden);
        await _proyectoRepository.GuardarCambiosAsync(ct);

        var tarea = proyecto.Tareas.First(t => t.Id == tareaId);
        return MapearADto(tarea);
    }

    public async Task EliminarAsync(Guid proyectoId, Guid tareaId, CancellationToken ct = default)
    {
        var proyecto = await ObtenerProyectoOFallarAsync(proyectoId, ct);

        proyecto.EliminarTarea(tareaId);
        await _proyectoRepository.GuardarCambiosAsync(ct);
    }

    private async Task<Proyecto> ObtenerProyectoOFallarAsync(Guid proyectoId, CancellationToken ct) =>
        await _proyectoRepository.ObtenerConTableroAsync(proyectoId, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", proyectoId);

    private async Task ValidarResponsableExisteAsync(Guid responsableId, CancellationToken ct)
    {
        var usuario = await _usuarioRepository.ObtenerPorIdAsync(responsableId, ct);
        if (usuario is null)
            throw new RecursoNoEncontradoException("Usuario", responsableId);
    }

    /// <summary>
    /// Orden de una tarea vecina, buscada específicamente dentro de la columna destino
    /// (una tarea puede estar cambiando de columna, así que no alcanza con buscarla por Id
    /// en todo el proyecto sin filtrar por columna).
    /// </summary>
    private static decimal? ObtenerOrdenTareaEnColumnaDestino(Proyecto proyecto, Guid columnaDestinoId, Guid? tareaId) =>
        tareaId is null
            ? null
            : proyecto.Tareas.FirstOrDefault(t => t.Id == tareaId && t.ColumnaId == columnaDestinoId)?.Orden;

    private static TareaDto MapearADto(Tarea tarea) => new(
        tarea.Id,
        tarea.Titulo,
        tarea.Descripcion,
        tarea.Prioridad,
        tarea.ResponsableId,
        tarea.ColumnaId,
        tarea.ProyectoId,
        tarea.Orden,
        tarea.FechaCreacion);
}
