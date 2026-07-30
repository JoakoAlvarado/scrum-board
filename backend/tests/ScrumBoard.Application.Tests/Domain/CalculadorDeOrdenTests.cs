using FluentAssertions;
using ScrumBoard.Domain.Services;
using Xunit;

namespace ScrumBoard.Application.Tests.Domain;

/// <summary>
/// Cubre el requisito obligatorio del enunciado (sección 6.9): al menos un test debe
/// validar el cálculo de la nueva posición de una tarea al reordenarla.
/// </summary>
public class CalculadorDeOrdenTests
{
    [Fact]
    public void Sin_vecinos_devuelve_el_gap_por_defecto()
    {
        var orden = CalculadorDeOrden.CalcularOrden(null, null);

        orden.Should().Be(1024m);
    }

    [Fact]
    public void Al_final_de_la_lista_suma_el_gap_al_ultimo_elemento()
    {
        var orden = CalculadorDeOrden.CalcularOrden(ordenAnterior: 1024m, ordenSiguiente: null);

        orden.Should().Be(2048m);
    }

    [Fact]
    public void Al_principio_de_la_lista_devuelve_la_mitad_del_primer_elemento()
    {
        var orden = CalculadorDeOrden.CalcularOrden(ordenAnterior: null, ordenSiguiente: 1024m);

        orden.Should().Be(512m);
    }

    [Fact]
    public void Entre_dos_elementos_devuelve_el_promedio()
    {
        var orden = CalculadorDeOrden.CalcularOrden(ordenAnterior: 1024m, ordenSiguiente: 2048m);

        orden.Should().Be(1536m);
    }

    [Fact]
    public void Si_los_vecinos_ya_no_dejan_espacio_para_un_promedio_distinto_lanza_excepcion()
    {
        // Caso límite: dos vecinos con el mismo valor de orden (ya sin espacio entre sí)
        // no permiten calcular una posición intermedia distinta; debe señalar que hace
        // falta un reindexado en vez de devolver un valor duplicado silenciosamente.
        var ordenAnterior = 1024m;
        var ordenSiguiente = 1024m;

        var accion = () => CalculadorDeOrden.CalcularOrden(ordenAnterior, ordenSiguiente);

        accion.Should().Throw<InvalidOperationException>();
    }
}
