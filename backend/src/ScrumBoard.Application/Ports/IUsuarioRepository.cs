using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Application.Ports;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
}
