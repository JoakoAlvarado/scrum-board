using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Services;

public interface IColumnaService
{
    Task<IReadOnlyList<ColumnaDto>> ListarAsync(Guid proyectoId, CancellationToken ct = default);
    Task<ColumnaDto> CrearAsync(Guid proyectoId, CrearColumnaRequest request, CancellationToken ct = default);
    Task<ColumnaDto> ActualizarAsync(Guid proyectoId, Guid columnaId, ActualizarColumnaRequest request, CancellationToken ct = default);
    Task<ColumnaDto> ReordenarAsync(Guid proyectoId, Guid columnaId, ReordenarColumnaRequest request, CancellationToken ct = default);
    Task EliminarAsync(Guid proyectoId, Guid columnaId, CancellationToken ct = default);
}
