namespace ScrumBoard.Domain.Entities;

/// <summary>
/// Usuario del sistema. El hash de password ya viene calculado desde la capa
/// de aplicación/infraestructura (BCrypt + pepper) — el dominio nunca conoce
/// el algoritmo de hashing, solo almacena el resultado.
/// </summary>
public class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime FechaCreacion { get; private set; }

    private Usuario() { } // EF Core

    public Usuario(string nombre, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));

        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FechaCreacion = DateTime.UtcNow;
    }
}
