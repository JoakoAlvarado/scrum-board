using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository) => _usuarioRepository = usuarioRepository;

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default)
    {
        var usuarios = await _usuarioRepository.ListarTodosAsync(ct);
        return usuarios.Select(u => new UsuarioDto(u.Id, u.Nombre, u.Email)).ToList();
    }
}
