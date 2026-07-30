using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Application.Ports;

public interface IJwtTokenGenerator
{
    /// <summary>Genera el token JWT y devuelve además su fecha de expiración (UTC).</summary>
    (string Token, DateTime ExpiraUtc) Generar(Usuario usuario);
}
