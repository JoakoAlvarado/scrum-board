using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;

namespace ScrumBoard.Application.Services;

/// <summary>
/// Orquesta el reporte: una sola consulta (<see cref="IReporteProyectoQuery"/>) arma el
/// DTO, y el <see cref="IReporteExporter"/> correspondiente al formato pedido lo convierte
/// a bytes (Strategy + Factory — requisito 6.8, ver docs/decisiones.md). Las distintas
/// implementaciones de IReporteExporter se registran todas bajo la misma interfaz en el
/// contenedor de DI; acá se resuelven como IEnumerable y se elige la que matchea el
/// formato pedido, sin un switch/if por formato.
/// </summary>
public class ReporteService : IReporteService
{
    private readonly IReporteProyectoQuery _query;
    private readonly IEnumerable<IReporteExporter> _exportadores;

    public ReporteService(IReporteProyectoQuery query, IEnumerable<IReporteExporter> exportadores)
    {
        _query = query;
        _exportadores = exportadores;
    }

    public async Task<ArchivoGeneradoDto> GenerarAsync(Guid proyectoId, FormatoReporte formato, CancellationToken ct = default)
    {
        var reporte = await _query.EjecutarAsync(proyectoId, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", proyectoId);

        var exportador = _exportadores.FirstOrDefault(e => e.Formato == formato)
            ?? throw new InvalidOperationException($"No hay un exportador registrado para el formato '{formato}'.");

        var contenido = exportador.Exportar(reporte);
        var nombreArchivo = $"reporte-{Sluggificar(reporte.NombreProyecto)}.{exportador.ExtensionArchivo}";

        return new ArchivoGeneradoDto(contenido, exportador.ContentType, nombreArchivo);
    }

    /// <summary>Nombre de archivo prolijo (requisito 6.8): sin espacios/acentos/mayúsculas.</summary>
    private static string Sluggificar(string nombre)
    {
        var sinAcentos = QuitarAcentos(nombre.Trim().ToLowerInvariant());
        var caracteres = sinAcentos.Select(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ? c : '-').ToArray();
        var slug = new string(caracteres);

        while (slug.Contains("--")) slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }

    private static string QuitarAcentos(string texto)
    {
        var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sinMarcas = normalizado.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);

        return new string(sinMarcas.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }
}
