using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Ports;

/// <summary>La única consulta que arma el DTO de reporte, consumida por ambos exportadores.</summary>
public interface IReporteProyectoQuery
{
    Task<ReporteProyectoDto?> EjecutarAsync(Guid proyectoId, CancellationToken ct = default);
}
