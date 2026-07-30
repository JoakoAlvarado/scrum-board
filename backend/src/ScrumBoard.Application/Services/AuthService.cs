using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Application.Services;

/// <summary>
/// Caso de uso de login. Nótese que no hay MediatR ni "handlers": es un servicio de
/// aplicación inyectado directamente en el controller — ver docs/decisiones.md,
/// sección "Sin MediatR".
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResultDto> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.ObtenerPorEmailAsync(email, ct)
            ?? throw new CredencialesInvalidasException();

        if (!_passwordHasher.Verify(password, usuario.PasswordHash))
            throw new CredencialesInvalidasException();

        var (token, expiraUtc) = _jwtTokenGenerator.Generar(usuario);

        return new LoginResultDto(usuario.Id, usuario.Nombre, usuario.Email, token, expiraUtc);
    }
}
