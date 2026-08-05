namespace ScrumBoard.Application.Dtos;

public record ColumnaDto(Guid Id, string Nombre, decimal Orden, Guid ProyectoId, int CantidadTareas);

public record CrearColumnaRequest(string Nombre);

public record ActualizarColumnaRequest(string Nombre);

/// <summary>
/// Reordena la columna posicionándola entre dos vecinas (por id). Null en un extremo
/// significa "al principio" o "al final" del tablero — ver CalculadorDeOrden.
/// </summary>
public record ReordenarColumnaRequest(Guid? ColumnaAnteriorId, Guid? ColumnaSiguienteId);
