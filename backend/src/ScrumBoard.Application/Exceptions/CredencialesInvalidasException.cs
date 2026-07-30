namespace ScrumBoard.Application.Exceptions;

/// <summary>Se lanza cuando el email no existe o el password no coincide. Se traduce a 401 en la Api.</summary>
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("Email o contraseña inválidos.") { }
}
