namespace ScrumBoard.Application.Dtos;

/// <summary>Proyección mínima de Usuario para poblar el selector de "responsable" en el frontend.</summary>
public record UsuarioDto(Guid Id, string Nombre, string Email);
