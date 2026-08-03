using ScrumBoard.Domain.Entities.Enums;

namespace ScrumBoard.Application.Dtos;

public record ProyectoDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    DateTime FechaInicio,
    DateTime FechaFinPrevista,
    EstadoProyecto Estado,
    int CantidadColumnas,
    int CantidadTareas);

public record CrearProyectoRequest(
    string Nombre,
    string Descripcion,
    DateTime FechaInicio,
    DateTime FechaFinPrevista);

public record ActualizarProyectoRequest(
    string Nombre,
    string Descripcion,
    DateTime FechaInicio,
    DateTime FechaFinPrevista,
    EstadoProyecto Estado);
