using ScrumBoard.Domain.Entities.Enums;

namespace ScrumBoard.Application.Dtos;

public record TareaDto(
    Guid Id,
    string Titulo,
    string Descripcion,
    Prioridad Prioridad,
    Guid ResponsableId,
    Guid ColumnaId,
    Guid ProyectoId,
    decimal Orden,
    DateTime FechaCreacion);

public record CrearTareaRequest(
    Guid ColumnaId,
    string Titulo,
    string Descripcion,
    Prioridad Prioridad,
    Guid ResponsableId);

public record ActualizarTareaRequest(
    string Titulo,
    string Descripcion,
    Prioridad Prioridad,
    Guid ResponsableId);

/// <summary>
/// Mueve la tarea a (posiblemente otra) columna, posicionándola entre dos vecinas de esa
/// columna destino. Null en un extremo significa "al principio" o "al final" de la columna.
/// </summary>
public record MoverTareaRequest(Guid ColumnaDestinoId, Guid? TareaAnteriorId, Guid? TareaSiguienteId);
