using Microsoft.EntityFrameworkCore;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Infrastructure.Persistence;

namespace ScrumBoard.Infrastructure.Repositories;

public class EfUsuarioRepository : IUsuarioRepository
{
    private readonly ScrumBoardDbContext _context;

    public EfUsuarioRepository(ScrumBoardDbContext context) => _context = context;

    public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), ct);

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct = default) =>
        await _context.Usuarios.AsNoTracking()
            .OrderBy(u => u.Nombre)
            .ToListAsync(ct);
}
