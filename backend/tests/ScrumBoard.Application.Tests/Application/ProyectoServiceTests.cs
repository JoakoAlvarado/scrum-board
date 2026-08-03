using FluentAssertions;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Application.Ports;
using ScrumBoard.Application.Services;
using ScrumBoard.Domain.Entities;
using Xunit;

namespace ScrumBoard.Application.Tests.Application;

public class ProyectoServiceTests
{
    /// <summary>
    /// Repositorio fake en memoria: prueba el caso de uso (ProyectoService) de forma
    /// aislada, sin levantar EF Core ni PostgreSQL.
    /// </summary>
    private class ProyectoRepositoryFake : IProyectoRepository
    {
        public readonly List<Proyecto> Proyectos = new();
        public bool GuardarCambiosFueLlamado { get; private set; }

        public Task<Proyecto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Proyectos.FirstOrDefault(p => p.Id == id));

        public Task<Proyecto?> ObtenerConTableroAsync(Guid id, CancellationToken ct = default) =>
            ObtenerPorIdAsync(id, ct);

        public Task<(IReadOnlyList<ProyectoDto> Items, int Total)> ListarPaginadoAsync(
            string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default)
        {
            var items = Proyectos
                .Select(p => new ProyectoDto(p.Id, p.Nombre, p.Descripcion, p.FechaInicio,
                    p.FechaFinPrevista, p.Estado, p.Columnas.Count, p.Tareas.Count))
                .ToList();

            return Task.FromResult((Items: (IReadOnlyList<ProyectoDto>)items, Total: items.Count));
        }

        public Task AgregarAsync(Proyecto proyecto, CancellationToken ct = default)
        {
            Proyectos.Add(proyecto);
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Proyecto proyecto, CancellationToken ct = default)
        {
            Proyectos.Remove(proyecto);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            GuardarCambiosFueLlamado = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CrearAsync_agrega_el_proyecto_y_guarda_cambios()
    {
        var repositorio = new ProyectoRepositoryFake();
        var service = new ProyectoService(repositorio);

        var request = new CrearProyectoRequest("Proyecto X", "desc", DateTime.UtcNow, DateTime.UtcNow.AddMonths(2));

        var resultado = await service.CrearAsync(request);

        repositorio.Proyectos.Should().ContainSingle(p => p.Id == resultado.Id);
        repositorio.GuardarCambiosFueLlamado.Should().BeTrue();
        resultado.Nombre.Should().Be("Proyecto X");
    }

    [Fact]
    public async Task ObtenerPorIdAsync_lanza_no_encontrado_si_no_existe()
    {
        var service = new ProyectoService(new ProyectoRepositoryFake());

        var accion = async () => await service.ObtenerPorIdAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task ListarAsync_limita_el_tamanio_de_pagina_al_maximo_permitido()
    {
        var repositorio = new ProyectoRepositoryFake();
        var service = new ProyectoService(repositorio);

        var resultado = await service.ListarAsync(filtroNombre: null, pagina: 1, tamanioPagina: 999);

        // 50 es el máximo definido en ProyectoService, para evitar listados sin cota
        // que pidan miles de filas en una sola consulta.
        resultado.TamanioPagina.Should().Be(50);
    }
}
