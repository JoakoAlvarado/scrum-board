using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Infrastructure.Reportes;

/// <summary>
/// Exportador PDF (requisito 6.8, obligatorio con QuestPDF). Una implementación más
/// de <see cref="IReporteExporter"/> (Strategy) — agregar un tercer formato no exige
/// tocar esta clase ni <see cref="ExcelReporteExporter"/>.
/// </summary>
public class PdfReporteExporter : IReporteExporter
{
    public FormatoReporte Formato => FormatoReporte.Pdf;
    public string ContentType => "application/pdf";
    public string ExtensionArchivo => "pdf";

    public byte[] Exportar(ReporteProyectoDto reporte)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(reporte.NombreProyecto).FontSize(18).Bold();

                    if (!string.IsNullOrWhiteSpace(reporte.Descripcion))
                        col.Item().PaddingTop(2).Text(reporte.Descripcion).FontSize(10).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingTop(6).Text($"Generado: {reporte.FechaGeneracionUtc:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Columna
                        columns.RelativeColumn(4); // Tarea
                        columns.RelativeColumn(2); // Responsable
                        columns.RelativeColumn(2); // Prioridad
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CeldaEncabezado).Text("Columna");
                        header.Cell().Element(CeldaEncabezado).Text("Tarea");
                        header.Cell().Element(CeldaEncabezado).Text("Responsable");
                        header.Cell().Element(CeldaEncabezado).Text("Prioridad");
                    });

                    foreach (var tarea in reporte.Tareas)
                    {
                        table.Cell().Element(CeldaContenido).Text(tarea.Columna);
                        table.Cell().Element(CeldaContenido).Text(tarea.Titulo);
                        table.Cell().Element(CeldaContenido).Text(tarea.Responsable);
                        table.Cell().Element(CeldaContenido).Text(tarea.Prioridad);
                    }

                    if (reporte.Tareas.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Element(CeldaContenido)
                            .Text("Este proyecto todavía no tiene tareas cargadas.")
                            .FontColor(Colors.Grey.Medium).Italic();
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static IContainer CeldaEncabezado(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(x => x.Bold());

    private static IContainer CeldaContenido(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
