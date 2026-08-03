using ScrumBoard.Application.Dtos;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Application.Ports;

public interface IProyectoRepository
{
    Task<Proyecto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Trae el proyecto junto con sus columnas y tareas (agregado completo), para operar el tablero.</summary>
    Task<Proyecto?> ObtenerConTableroAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Devuelve directamente una proyección de solo lectura (no el agregado completo):
    /// listar no necesita cargar todas las columnas/tareas en memoria para contar cuántas
    /// hay, alcanza con que EF Core traduzca el conteo a SQL. Ver docs/decisiones.md.
    /// </summary>
    Task<(IReadOnlyList<ProyectoDto> Items, int Total)> ListarPaginadoAsync(
        string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default);

    Task AgregarAsync(Proyecto proyecto, CancellationToken ct = default);
    Task EliminarAsync(Proyecto proyecto, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
