using Microsoft.AspNetCore.SignalR;
using ScrumBoard.Api.Hubs;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Api.Realtime;

/// <summary>
/// Adaptador de <see cref="IRealtimeNotifier"/> sobre SignalR. Vive en Api (no en
/// Infrastructure) porque necesita <see cref="IHubContext{THub}"/>, que ya está
/// disponible de forma nativa en un proyecto Web SDK sin agregar un paquete NuGet
/// extra en Infrastructure — ver docs/decisiones.md, sección "Tiempo real".
/// </summary>
public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<TableroHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<TableroHub> hubContext) => _hubContext = hubContext;

    public Task NotificarTareaCreadaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "TareaCreada", tarea, ct);

    public Task NotificarTareaActualizadaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "TareaActualizada", tarea, ct);

    public Task NotificarTareaMovidaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "TareaMovida", tarea, ct);

    public Task NotificarTareaEliminadaAsync(Guid proyectoId, Guid tareaId, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "TareaEliminada", tareaId, ct);

    public Task NotificarColumnaCreadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "ColumnaCreada", columna, ct);

    public Task NotificarColumnaActualizadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "ColumnaActualizada", columna, ct);

    public Task NotificarColumnaReordenadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "ColumnaReordenada", columna, ct);

    public Task NotificarColumnaEliminadaAsync(Guid proyectoId, Guid columnaId, CancellationToken ct = default) =>
        EnviarAsync(proyectoId, "ColumnaEliminada", columnaId, ct);

    private Task EnviarAsync(Guid proyectoId, string metodo, object payload, CancellationToken ct) =>
        _hubContext.Clients.Group(TableroHub.NombreGrupo(proyectoId)).SendAsync(metodo, payload, ct);
}
