namespace ScrumBoard.Infrastructure.Security;

/// <summary>
/// El "pepper" es un secreto de aplicación (no de fila) que se concatena al password
/// antes de aplicar BCrypt. Vive solo en configuración/variables de entorno, nunca en
/// la base de datos ni en el repositorio — ver docs/decisiones.md, sección
/// "Hash de contraseña".
/// </summary>
public class PasswordHasherOptions
{
    public const string SeccionConfig = "PasswordHasher";

    public string Pepper { get; set; } = string.Empty;
}
