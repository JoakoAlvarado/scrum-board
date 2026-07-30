namespace ScrumBoard.Domain.Exceptions;

/// <summary>
/// Excepción para violaciones de reglas de negocio del dominio.
/// Se traduce en la capa Api a un 400/409 según corresponda (no expone detalles internos).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
