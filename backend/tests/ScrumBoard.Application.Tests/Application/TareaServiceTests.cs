using FluentAssertions;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Ports;
using ScrumBoard.Application.Services;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Domain.Entities.Enums;
using Xunit;

namespace ScrumBoard.Application.Tests.Application;

public class TareaServiceTests
{
    private class ProyectoRepositoryFake : IProyectoRepository
    {
        public Proyecto? Proyecto;
        public bool GuardarCambiosFueLlamado { get; private set; }

        public Task<Proyecto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Proyecto?.Id == id ? Proyecto : null);

        public Task<Proyecto?> ObtenerConTableroAsync(Guid id, CancellationToken ct = default) =>
            ObtenerPorIdAsync(id, ct);

        public Task<(IReadOnlyList<ProyectoDto> Items, int Total)> ListarPaginadoAsync(
            string? filtroNombre, int pagina, int tamanioPagina, CancellationToken ct = default) =>
            throw new NotImplementedException("No usado en estos tests.");

        public Task AgregarAsync(Proyecto proyecto, CancellationToken ct = default)
        {
            Proyecto = proyecto;
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Proyecto proyecto, CancellationToken ct = default) => Task.CompletedTask;

        public Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            GuardarCambiosFueLlamado = true;
            return Task.CompletedTask;
        }
    }

    private class UsuarioRepositoryFake : IUsuarioRepository
    {
        public readonly Usuario Usuario = new("Responsable Demo", "responsable@demo.local", "hash-no-relevante");

        public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult<Usuario?>(Usuario);

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(id == Usuario.Id ? Usuario : null);

        public Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>(new List<Usuario> { Usuario });
    }

    private static (ProyectoRepositoryFake Proyectos, UsuarioRepositoryFake Usuarios, TareaService Service, Proyecto Proyecto, Columna Columna)
        CrearEscenario()
    {
        var proyecto = new Proyecto("Proyecto X", "desc", DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        var columna = proyecto.AgregarColumna("To Do", 1024m);

        var proyectoRepo = new ProyectoRepositoryFake { Proyecto = proyecto };
        var usuarioRepo = new UsuarioRepositoryFake();
        var service = new TareaService(proyectoRepo, usuarioRepo);

        return (proyectoRepo, usuarioRepo, service, proyecto, columna);
    }

    [Fact]
    public async Task CrearAsync_agrega_la_tarea_al_final_de_la_columna()
    {
        var (proyectoRepo, usuarios, service, proyecto, columna) = CrearEscenario();

        var request = new CrearTareaRequest(columna.Id, "Tarea 1", "desc", Prioridad.Alta, usuarios.Usuario.Id);
        var resultado = await service.CrearAsync(proyecto.Id, request);

        resultado.Orden.Should().Be(1024m); // primera tarea de una columna vacía: gap por defecto
        proyectoRepo.GuardarCambiosFueLlamado.Should().BeTrue();
    }

    [Fact]
    public async Task MoverAsync_calcula_el_orden_entre_dos_tareas_vecinas_en_la_columna_destino()
    {
        var (_, usuarios, service, proyecto, columnaOrigen) = CrearEscenario();
        var columnaDestino = proyecto.AgregarColumna("En progreso", 2048m);

        // Dos tareas ya ubicadas en la columna destino, con orden 1024 y 2048.
        var t1 = proyecto.AgregarTarea(columnaDestino.Id, "Tarea A", "", Prioridad.Media, usuarios.Usuario.Id, 1024m);
        var t2 = proyecto.AgregarTarea(columnaDestino.Id, "Tarea B", "", Prioridad.Media, usuarios.Usuario.Id, 2048m);

        // La tarea a mover está en la columna origen.
        var tareaAMover = proyecto.AgregarTarea(columnaOrigen.Id, "Tarea a mover", "", Prioridad.Baja, usuarios.Usuario.Id, 1024m);

        var request = new MoverTareaRequest(columnaDestino.Id, TareaAnteriorId: t1.Id, TareaSiguienteId: t2.Id);
        var resultado = await service.MoverAsync(proyecto.Id, tareaAMover.Id, request);

        resultado.ColumnaId.Should().Be(columnaDestino.Id);
        resultado.Orden.Should().Be(1536m); // promedio entre 1024 y 2048 — mismo cálculo que CalculadorDeOrdenTests
    }
}
