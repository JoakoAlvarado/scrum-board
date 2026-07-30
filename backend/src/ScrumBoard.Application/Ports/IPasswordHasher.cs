namespace ScrumBoard.Application.Ports;

/// <summary>
/// Puerto para el hashing de contraseñas. La implementación concreta (BCrypt + pepper)
/// vive en Infrastructure — ver docs/decisiones.md, sección "Hash de contraseña".
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
