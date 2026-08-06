using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Ports;

/// <summary>
/// Puerto para notificar eventos del tablero en tiempo real (requisito 6.7). La
/// implementación concreta (SignalR Hub) vive en la capa Api — ver docs/decisiones.md,
/// sección "Tiempo real". Application/Domain no conocen SignalR, solo este contrato.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotificarTareaCreadaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default);
    Task NotificarTareaActualizadaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default);
    Task NotificarTareaMovidaAsync(Guid proyectoId, TareaDto tarea, CancellationToken ct = default);
    Task NotificarTareaEliminadaAsync(Guid proyectoId, Guid tareaId, CancellationToken ct = default);

    Task NotificarColumnaCreadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default);
    Task NotificarColumnaActualizadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default);
    Task NotificarColumnaReordenadaAsync(Guid proyectoId, ColumnaDto columna, CancellationToken ct = default);
    Task NotificarColumnaEliminadaAsync(Guid proyectoId, Guid columnaId, CancellationToken ct = default);
}
