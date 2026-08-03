using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Application.Services;

/// <summary>
/// Caso de uso de gestión de Proyectos. Sin MediatR: es un servicio de aplicación
/// inyectado directamente en el controller — ver docs/decisiones.md.
/// </summary>
public class ProyectoService : IProyectoService
{
    private const int TamanioPaginaPorDefecto = 10;
    private const int TamanioPaginaMaximo = 50;

    private readonly IProyectoRepository _proyectoRepository;

    public ProyectoService(IProyectoRepository proyectoRepository)
    {
        _proyectoRepository = proyectoRepository;
    }

    public async Task<PagedResultDto<ProyectoDto>> ListarAsync(
        string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanioPagina = tamanioPagina < 1 ? TamanioPaginaPorDefecto : Math.Min(tamanioPagina, TamanioPaginaMaximo);

        var (items, total) = await _proyectoRepository.ListarPaginadoAsync(filtroNombre, pagina, tamanioPagina, ct);

        return new PagedResultDto<ProyectoDto>(items, total, pagina, tamanioPagina);
    }

    public async Task<ProyectoDto> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var proyecto = await _proyectoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", id);

        return MapearADto(proyecto);
    }

    public async Task<ProyectoDto> CrearAsync(CrearProyectoRequest request, CancellationToken ct = default)
    {
        var proyecto = new Proyecto(request.Nombre, request.Descripcion, request.FechaInicio, request.FechaFinPrevista);

        await _proyectoRepository.AgregarAsync(proyecto, ct);
        await _proyectoRepository.GuardarCambiosAsync(ct);

        return MapearADto(proyecto);
    }

    public async Task<ProyectoDto> ActualizarAsync(Guid id, ActualizarProyectoRequest request, CancellationToken ct = default)
    {
        var proyecto = await _proyectoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", id);

        proyecto.Actualizar(request.Nombre, request.Descripcion, request.FechaInicio, request.FechaFinPrevista);
        proyecto.CambiarEstado(request.Estado);

        await _proyectoRepository.GuardarCambiosAsync(ct);

        return MapearADto(proyecto);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var proyecto = await _proyectoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("Proyecto", id);

        await _proyectoRepository.EliminarAsync(proyecto, ct);
        await _proyectoRepository.GuardarCambiosAsync(ct);
    }

    private static ProyectoDto MapearADto(Proyecto proyecto) => new(
        proyecto.Id,
        proyecto.Nombre,
        proyecto.Descripcion,
        proyecto.FechaInicio,
        proyecto.FechaFinPrevista,
        proyecto.Estado,
        proyecto.Columnas.Count,
        proyecto.Tareas.Count);
}
