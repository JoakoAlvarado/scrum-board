namespace ScrumBoard.Application.Dtos;

/// <summary>Resultado listo para devolver por HTTP: bytes + metadata de descarga.</summary>
public record ArchivoGeneradoDto(byte[] Contenido, string ContentType, string NombreArchivo);
