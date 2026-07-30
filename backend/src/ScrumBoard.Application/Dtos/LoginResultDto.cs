namespace ScrumBoard.Application.Dtos;

public record LoginResultDto(Guid UsuarioId, string Nombre, string Email, string Token, DateTime ExpiraUtc);
