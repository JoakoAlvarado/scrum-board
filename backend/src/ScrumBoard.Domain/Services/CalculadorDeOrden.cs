namespace ScrumBoard.Domain.Services;

/// <summary>
/// Calcula la posición (orden fraccionario) de una tarea o columna al insertarla
/// o moverla entre dos vecinos existentes, sin necesidad de reindexar el resto
/// de los elementos de la lista. Ver docs/decisiones.md — "Estrategia de orden".
/// </summary>
public static class CalculadorDeOrden
{
    /// <summary>
    /// Gap usado cuando no hay un vecino de un lado (insertar al principio o al final
    /// de una lista vacía o en un extremo).
    /// </summary>
    private const decimal GapPorDefecto = 1024m;

    /// <summary>
    /// Calcula el nuevo valor de orden dado el orden del elemento inmediatamente
    /// anterior y el del inmediatamente siguiente en la posición destino.
    /// </summary>
    /// <param name="ordenAnterior">Orden del elemento que queda antes, o null si se inserta al inicio.</param>
    /// <param name="ordenSiguiente">Orden del elemento que queda después, o null si se inserta al final.</param>
    public static decimal CalcularOrden(decimal? ordenAnterior, decimal? ordenSiguiente)
    {
        if (ordenAnterior is null && ordenSiguiente is null)
            return GapPorDefecto; // primera tarea de una columna vacía

        if (ordenAnterior is null)
            return ordenSiguiente!.Value / 2m; // se inserta antes de todo lo existente

        if (ordenSiguiente is null)
            return ordenAnterior!.Value + GapPorDefecto; // se inserta al final

        var nuevoOrden = (ordenAnterior.Value + ordenSiguiente.Value) / 2m;

        if (nuevoOrden == ordenAnterior.Value || nuevoOrden == ordenSiguiente.Value)
        {
            throw new InvalidOperationException(
                "El espacio entre los elementos vecinos es demasiado pequeño para insertar " +
                "un nuevo orden fraccionario. Es necesario reindexar la columna/lista.");
        }

        return nuevoOrden;
    }
}
