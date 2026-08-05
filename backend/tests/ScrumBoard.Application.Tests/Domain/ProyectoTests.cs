using FluentAssertions;
using ScrumBoard.Domain.Entities;
using ScrumBoard.Domain.Entities.Enums;
using ScrumBoard.Domain.Exceptions;
using Xunit;

namespace ScrumBoard.Application.Tests.Domain;

public class ProyectoTests
{
    private static Proyecto CrearProyecto() =>
        new("Proyecto de prueba", "descripción", DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

    [Fact]
    public void No_permite_eliminar_una_columna_que_contiene_tareas()
    {
        var proyecto = CrearProyecto();
        var columna = proyecto.AgregarColumna("To Do", 1024m);
        proyecto.AgregarTarea(columna.Id, "Tarea 1", "desc", Prioridad.Media, Guid.NewGuid(), 1024m);

        var accion = () => proyecto.EliminarColumna(columna.Id);

        accion.Should().Throw<DomainException>()
            .WithMessage("*No se puede eliminar una columna que contiene tareas*",
                because: "el enunciado exige explícitamente esta regla de negocio");
    }

    [Fact]
    public void Permite_eliminar_una_columna_vacia()
    {
        var proyecto = CrearProyecto();
        var columna = proyecto.AgregarColumna("To Do", 1024m);

        proyecto.EliminarColumna(columna.Id);

        proyecto.Columnas.Should().NotContain(c => c.Id == columna.Id);
    }

    [Fact]
    public void No_permite_mover_una_tarea_a_una_columna_de_otro_proyecto()
    {
        var proyecto = CrearProyecto();
        var columnaOrigen = proyecto.AgregarColumna("To Do", 1024m);
        var tarea = proyecto.AgregarTarea(columnaOrigen.Id, "Tarea 1", "desc", Prioridad.Media, Guid.NewGuid(), 1024m);

        var columnaDeOtroProyecto = Guid.NewGuid(); // no pertenece a este agregado

        var accion = () => proyecto.MoverTarea(tarea.Id, columnaDeOtroProyecto, 2048m);

        accion.Should().Throw<DomainException>(
            because: "mantiene consistente el campo denormalizado Tarea.ProyectoId");
    }

    [Fact]
    public void EliminarTarea_la_quita_del_proyecto()
    {
        var proyecto = CrearProyecto();
        var columna = proyecto.AgregarColumna("To Do", 1024m);
        var tarea = proyecto.AgregarTarea(columna.Id, "Tarea 1", "desc", Prioridad.Media, Guid.NewGuid(), 1024m);

        proyecto.EliminarTarea(tarea.Id);

        proyecto.Tareas.Should().NotContain(t => t.Id == tarea.Id);
    }

    [Fact]
    public void EliminarTarea_de_un_id_inexistente_lanza_excepcion()
    {
        var proyecto = CrearProyecto();

        var accion = () => proyecto.EliminarTarea(Guid.NewGuid());

        accion.Should().Throw<DomainException>();
    }

    [Fact]
    public void RenombrarColumna_actualiza_el_nombre()
    {
        var proyecto = CrearProyecto();
        var columna = proyecto.AgregarColumna("To Do", 1024m);

        proyecto.RenombrarColumna(columna.Id, "En progreso");

        proyecto.Columnas.Single(c => c.Id == columna.Id).Nombre.Should().Be("En progreso");
    }
}
