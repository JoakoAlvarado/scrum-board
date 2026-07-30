using Microsoft.Extensions.Options;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    private readonly string _pepper;

    public BCryptPasswordHasher(IOptions<PasswordHasherOptions> options)
    {
        _pepper = options.Value.Pepper;

        if (string.IsNullOrWhiteSpace(_pepper))
            throw new InvalidOperationException(
                "PasswordHasher:Pepper no está configurado. Definí PEPPER en las variables de entorno.");
    }

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(ConPepper(password), workFactor: 12);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(ConPepper(password), passwordHash);

    private string ConPepper(string password) => password + _pepper;
}
