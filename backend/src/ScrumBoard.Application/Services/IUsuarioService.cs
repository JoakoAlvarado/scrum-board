using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Services;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default);
}
