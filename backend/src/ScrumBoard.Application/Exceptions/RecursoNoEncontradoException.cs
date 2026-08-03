namespace ScrumBoard.Application.Exceptions;

/// <summary>Se lanza cuando un recurso solicitado por Id no existe. Se traduce a 404 en la Api.</summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string recurso, Guid id)
        : base($"{recurso} con id '{id}' no fue encontrado.") { }
}
