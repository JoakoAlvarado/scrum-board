namespace ScrumBoard.Application.Ports;

/// <summary>
/// Puerto para notificar eventos del tablero en tiempo real. La implementación
/// concreta (SignalR Hub) vive en la capa Api — ver docs/decisiones.md, sección
/// "Tiempo real". El dominio/aplicación no conoce SignalR, solo este contrato.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotificarTareaCreadaAsync(Guid proyectoId, object payload, CancellationToken ct = default);
    Task NotificarTareaActualizadaAsync(Guid proyectoId, object payload, CancellationToken ct = default);
    Task NotificarTareaEliminadaAsync(Guid proyectoId, Guid tareaId, CancellationToken ct = default);
    Task NotificarTareaMovidaAsync(Guid proyectoId, object payload, CancellationToken ct = default);
}
