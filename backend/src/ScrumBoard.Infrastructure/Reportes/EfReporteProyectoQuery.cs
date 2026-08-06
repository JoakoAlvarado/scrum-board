using Microsoft.EntityFrameworkCore;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;
using ScrumBoard.Infrastructure.Persistence;

namespace ScrumBoard.Infrastructure.Reportes;

/// <summary>
/// Única consulta que arma el DTO de reporte, consumida por ambos exportadores
/// (requisito 6.8). Ver docs/decisiones.md, sección "Reportes", sobre por qué son dos
/// round-trips (encabezado del proyecto + tareas) y no un único JOIN.
/// </summary>
public class EfReporteProyectoQuery : IReporteProyectoQuery
{
    private readonly ScrumBoardDbContext _context;

    public EfReporteProyectoQuery(ScrumBoardDbContext context) => _context = context;

    public async Task<ReporteProyectoDto?> EjecutarAsync(Guid proyectoId, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos.AsNoTracking()
            .Where(p => p.Id == proyectoId)
            .Select(p => new { p.Nombre, p.Descripcion })
            .FirstOrDefaultAsync(ct);

        if (proyecto is null) return null;

        var tareas = await (
            from t in _context.Tareas.AsNoTracking()
            join c in _context.Columnas.AsNoTracking() on t.ColumnaId equals c.Id
            join u in _context.Usuarios.AsNoTracking() on t.ResponsableId equals u.Id
            where t.ProyectoId == proyectoId
            orderby c.Orden, t.Orden
            select new TareaReporteDto(c.Nombre, t.Titulo, u.Nombre, t.Prioridad.ToString())
        ).ToListAsync(ct);

        return new ReporteProyectoDto(proyecto.Nombre, proyecto.Descripcion, DateTime.UtcNow, tareas);
    }
}
