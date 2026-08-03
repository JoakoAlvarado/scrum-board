namespace ScrumBoard.Application.Dtos;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanioPagina);
