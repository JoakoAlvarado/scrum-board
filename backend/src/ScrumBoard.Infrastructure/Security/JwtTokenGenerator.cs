using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Secret))
            throw new InvalidOperationException(
                "Jwt:Secret no está configurado. Definí JWT_SECRET en las variables de entorno.");
    }

    public (string Token, DateTime ExpiraUtc) Generar(Usuario usuario)
    {
        var expiraUtc = DateTime.UtcNow.AddMinutes(_options.ExpiracionMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraUtc,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraUtc);
    }
}
