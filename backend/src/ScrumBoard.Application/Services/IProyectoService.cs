using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Services;

public interface IProyectoService
{
    Task<PagedResultDto<ProyectoDto>> ListarAsync(
        string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default);

    Task<ProyectoDto> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    Task<ProyectoDto> CrearAsync(CrearProyectoRequest request, CancellationToken ct = default);

    Task<ProyectoDto> ActualizarAsync(Guid id, ActualizarProyectoRequest request, CancellationToken ct = default);

    Task EliminarAsync(Guid id, CancellationToken ct = default);
}
