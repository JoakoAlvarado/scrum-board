using ScrumBoard.Application.Dtos;
using ScrumBoard.Domain.Entities.Enums;

namespace ScrumBoard.Application.Services;

public interface ITareaService
{
    /// <summary>Filtros opcionales por columna, responsable y prioridad (requisito deseable 7).</summary>
    Task<IReadOnlyList<TareaDto>> ListarAsync(
        Guid proyectoId, Guid? columnaId, Guid? responsableId, Prioridad? prioridad, CancellationToken ct = default);

    Task<TareaDto> CrearAsync(Guid proyectoId, CrearTareaRequest request, CancellationToken ct = default);
    Task<TareaDto> ActualizarAsync(Guid proyectoId, Guid tareaId, ActualizarTareaRequest request, CancellationToken ct = default);
    Task<TareaDto> MoverAsync(Guid proyectoId, Guid tareaId, MoverTareaRequest request, CancellationToken ct = default);
    Task EliminarAsync(Guid proyectoId, Guid tareaId, CancellationToken ct = default);
}
