using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Ports;
using ScrumBoard.Application.Services;

namespace ScrumBoard.Api.Controllers;

/// <summary>Descarga del reporte del proyecto en PDF o Excel (requisito 6.8).</summary>
[ApiController]
[Authorize]
[Route("api/proyectos/{proyectoId:guid}/reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService) => _reporteService = reporteService;

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(Guid proyectoId, CancellationToken ct)
    {
        var archivo = await _reporteService.GenerarAsync(proyectoId, FormatoReporte.Pdf, ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(Guid proyectoId, CancellationToken ct)
    {
        var archivo = await _reporteService.GenerarAsync(proyectoId, FormatoReporte.Excel, ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
