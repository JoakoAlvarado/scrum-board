using FluentAssertions;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Infrastructure.Reportes;
using Xunit;

namespace ScrumBoard.Application.Tests.Infrastructure;

/// <summary>
/// Ejercita los exportadores reales (no un fake) para verificar que la misma
/// ReporteProyectoDto efectivamente produce bytes válidos en ambos formatos —
/// requisito 6.8.
/// </summary>
public class ReporteExportersTests
{
    static ReporteExportersTests()
    {
        // QuestPDF exige declarar la licencia antes de generar cualquier documento.
        // En la Api esto se hace una vez en Program.cs; en los tests hay que repetirlo
        // porque Program.cs no se ejecuta.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static ReporteProyectoDto CrearReporteDeEjemplo() => new(
        NombreProyecto: "Sprint Ágil #1",
        Descripcion: "Proyecto de prueba para el reporte",
        FechaGeneracionUtc: new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc),
        Tareas: new List<TareaReporteDto>
        {
            new("To Do", "Diseñar el modelo de datos", "Ana", "Alta"),
            new("En progreso", "Implementar el login", "Beto", "Urgente")
        });

    [Fact]
    public void PdfReporteExporter_genera_un_pdf_valido_y_no_vacio()
    {
        var exporter = new PdfReporteExporter();

        var bytes = exporter.Exportar(CrearReporteDeEjemplo());

        bytes.Should().NotBeEmpty();
        // Todo archivo PDF empieza con esta firma ("%PDF-").
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        exporter.ContentType.Should().Be("application/pdf");
        exporter.ExtensionArchivo.Should().Be("pdf");
    }

    [Fact]
    public void ExcelReporteExporter_genera_un_xlsx_valido_y_no_vacio()
    {
        var exporter = new ExcelReporteExporter();

        var bytes = exporter.Exportar(CrearReporteDeEjemplo());

        bytes.Should().NotBeEmpty();
        // .xlsx es un .zip: todo archivo zip empieza con la firma "PK".
        bytes[0].Should().Be((byte)'P');
        bytes[1].Should().Be((byte)'K');
        exporter.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        exporter.ExtensionArchivo.Should().Be("xlsx");
    }

    [Fact]
    public void Ambos_exportadores_manejan_un_proyecto_sin_tareas_sin_explotar()
    {
        var reporteVacio = new ReporteProyectoDto("Proyecto vacío", "", DateTime.UtcNow, new List<TareaReporteDto>());

        var pdf = () => new PdfReporteExporter().Exportar(reporteVacio);
        var excel = () => new ExcelReporteExporter().Exportar(reporteVacio);

        pdf.Should().NotThrow();
        excel.Should().NotThrow();
    }
}
