using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Ports;

public enum FormatoReporte
{
    Pdf,
    Excel
}

/// <summary>
/// Puerto de exportación. Cada formato es una implementación separada (Strategy);
/// agregar un tercer formato es una clase nueva sin tocar las existentes (Open/Closed) —
/// ver docs/decisiones.md, sección "Exportación dual".
/// </summary>
public interface IReporteExporter
{
    FormatoReporte Formato { get; }
    string ContentType { get; }
    string ExtensionArchivo { get; }
    byte[] Exportar(ReporteProyectoDto reporte);
}
