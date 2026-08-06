using ClosedXML.Excel;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Infrastructure.Reportes;

/// <summary>
/// Exportador Excel (requisito 6.8, librería a elección — ver docs/decisiones.md, día 1,
/// por qué ClosedXML y no EPPlus). Misma <see cref="ReporteProyectoDto"/> que
/// <see cref="PdfReporteExporter"/>, sin volver a consultar la base de datos.
/// </summary>
public class ExcelReporteExporter : IReporteExporter
{
    public FormatoReporte Formato => FormatoReporte.Excel;
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string ExtensionArchivo => "xlsx";

    public byte[] Exportar(ReporteProyectoDto reporte)
    {
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Reporte");

        hoja.Cell(1, 1).Value = reporte.NombreProyecto;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Range(1, 1, 1, 4).Merge();

        if (!string.IsNullOrWhiteSpace(reporte.Descripcion))
        {
            hoja.Cell(2, 1).Value = reporte.Descripcion;
            hoja.Range(2, 1, 2, 4).Merge();
        }

        hoja.Cell(3, 1).Value = $"Generado: {reporte.FechaGeneracionUtc:dd/MM/yyyy HH:mm} UTC";
        hoja.Cell(3, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);
        hoja.Range(3, 1, 3, 4).Merge();

        const int filaEncabezado = 5;
        string[] encabezados = { "Columna", "Tarea", "Responsable", "Prioridad" };

        for (var i = 0; i < encabezados.Length; i++)
        {
            var celda = hoja.Cell(filaEncabezado, i + 1);
            celda.Value = encabezados[i];
            celda.Style.Font.SetBold();
            celda.Style.Fill.SetBackgroundColor(XLColor.LightGray);
        }

        var fila = filaEncabezado + 1;
        foreach (var tarea in reporte.Tareas)
        {
            hoja.Cell(fila, 1).Value = tarea.Columna;
            hoja.Cell(fila, 2).Value = tarea.Titulo;
            hoja.Cell(fila, 3).Value = tarea.Responsable;
            hoja.Cell(fila, 4).Value = tarea.Prioridad;
            fila++;
        }

        // Anchos legibles (requisito 6.8) — AdjustToContents ajusta según el contenido
        // real; se fuerza además un mínimo para "Tarea", que suele tener títulos largos.
        hoja.Columns(1, 4).AdjustToContents();
        if (hoja.Column(2).Width < 30) hoja.Column(2).Width = 30;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
