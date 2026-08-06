using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Application.Services;

public interface IReporteService
{
    Task<ArchivoGeneradoDto> GenerarAsync(Guid proyectoId, FormatoReporte formato, CancellationToken ct = default);
}
