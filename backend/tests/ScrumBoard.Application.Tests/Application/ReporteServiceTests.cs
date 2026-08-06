using FluentAssertions;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;
using ScrumBoard.Application.Services;
using Xunit;

namespace ScrumBoard.Application.Tests.Application;

public class ReporteServiceTests
{
    private class ReporteQueryFake : IReporteProyectoQuery
    {
        public ReporteProyectoDto? Reporte;

        public Task<ReporteProyectoDto?> EjecutarAsync(Guid proyectoId, CancellationToken ct = default) =>
            Task.FromResult(Reporte);
    }

    private class ExporterFake : IReporteExporter
    {
        public ExporterFake(FormatoReporte formato, string contentType, string extension)
        {
            Formato = formato;
            ContentType = contentType;
            ExtensionArchivo = extension;
        }

        public FormatoReporte Formato { get; }
        public string ContentType { get; }
        public string ExtensionArchivo { get; }
        public int VecesLlamado { get; private set; }

        public byte[] Exportar(ReporteProyectoDto reporte)
        {
            VecesLlamado++;
            return new byte[] { 1, 2, 3 };
        }
    }

    [Fact]
    public async Task GenerarAsync_elige_el_exportador_que_matchea_el_formato_pedido()
    {
        var query = new ReporteQueryFake
        {
            Reporte = new ReporteProyectoDto("Proyecto", "desc", DateTime.UtcNow, new List<TareaReporteDto>())
        };
        var exporterPdf = new ExporterFake(FormatoReporte.Pdf, "application/pdf", "pdf");
        var exporterExcel = new ExporterFake(FormatoReporte.Excel, "application/vnd...sheet", "xlsx");

        var service = new ReporteService(query, new[] { exporterPdf, exporterExcel });

        var resultado = await service.GenerarAsync(Guid.NewGuid(), FormatoReporte.Excel);

        resultado.NombreArchivo.Should().EndWith(".xlsx");
        exporterExcel.VecesLlamado.Should().Be(1);
        exporterPdf.VecesLlamado.Should().Be(0, because: "solo se pidió el formato Excel");
    }

    [Fact]
    public async Task GenerarAsync_lanza_no_encontrado_si_el_proyecto_no_existe()
    {
        var query = new ReporteQueryFake { Reporte = null };
        var service = new ReporteService(query, new[] { new ExporterFake(FormatoReporte.Pdf, "application/pdf", "pdf") });

        var accion = async () => await service.GenerarAsync(Guid.NewGuid(), FormatoReporte.Pdf);

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task GenerarAsync_sluggifica_el_nombre_del_archivo()
    {
        var query = new ReporteQueryFake
        {
            Reporte = new ReporteProyectoDto("Sprint Ágil Nº 1!!", "", DateTime.UtcNow, new List<TareaReporteDto>())
        };
        var service = new ReporteService(query, new[] { new ExporterFake(FormatoReporte.Pdf, "application/pdf", "pdf") });

        var resultado = await service.GenerarAsync(Guid.NewGuid(), FormatoReporte.Pdf);

        resultado.NombreArchivo.Should().MatchRegex("^reporte-[a-z0-9-]+\\.pdf$");
    }
}
