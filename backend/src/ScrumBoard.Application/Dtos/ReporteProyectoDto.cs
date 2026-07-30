namespace ScrumBoard.Application.Dtos;

/// <summary>
/// Única estructura de transferencia que alimenta tanto el exportador PDF como el Excel.
/// Se construye con una sola consulta (ver puerto IReporteProyectoQuery) — ver
/// docs/decisiones.md, sección "Exportación dual".
/// </summary>
public record ReporteProyectoDto(
    string NombreProyecto,
    string Descripcion,
    DateTime FechaGeneracionUtc,
    IReadOnlyList<TareaReporteDto> Tareas);

public record TareaReporteDto(
    string Columna,
    string Titulo,
    string Responsable,
    string Prioridad);
