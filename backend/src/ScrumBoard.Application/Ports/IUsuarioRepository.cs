using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Application.Ports;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lista liviana de todos los usuarios, para poblar el selector de "responsable".</summary>
    Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct = default);
}
