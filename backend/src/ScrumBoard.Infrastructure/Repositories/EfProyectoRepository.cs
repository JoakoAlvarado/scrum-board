using Microsoft.EntityFrameworkCore;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Infrastructure.Persistence;

namespace ScrumBoard.Infrastructure.Repositories;

public class EfProyectoRepository : IProyectoRepository
{
    private readonly ScrumBoardDbContext _context;

    public EfProyectoRepository(ScrumBoardDbContext context) => _context = context;

    public Task<Proyecto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Proyectos
            .Include(p => p.Columnas)
            .Include(p => p.Tareas)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Proyecto?> ObtenerConTableroAsync(Guid id, CancellationToken ct = default) =>
        _context.Proyectos
            .Include(p => p.Columnas)
            .Include(p => p.Tareas)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<ProyectoDto> Items, int Total)> ListarPaginadoAsync(
        string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default)
    {
        var query = _context.Proyectos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtroNombre))
        {
            // Coincidencia parcial resuelta en el servidor (requisito 6.3), case-insensitive.
            var patron = $"%{filtroNombre.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Nombre, patron));
        }

        var total = await query.CountAsync(ct);

        // Proyección directa a DTO: el conteo de columnas/tareas se resuelve en SQL
        // (subquery de Count), sin materializar esas colecciones completas en memoria.
        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(p => new ProyectoDto(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.FechaInicio,
                p.FechaFinPrevista,
                p.Estado,
                p.Columnas.Count,
                p.Tareas.Count))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AgregarAsync(Proyecto proyecto, CancellationToken ct = default) =>
        await _context.Proyectos.AddAsync(proyecto, ct);

    public Task EliminarAsync(Proyecto proyecto, CancellationToken ct = default)
    {
        _context.Proyectos.Remove(proyecto);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
