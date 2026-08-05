namespace ScrumBoard.Domain.Entities;

public class Columna
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public decimal Orden { get; private set; }
    public Guid ProyectoId { get; private set; }

    private Columna() { } // EF Core

    internal Columna(Guid proyectoId, string nombre, decimal orden)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la columna es obligatorio.", nameof(nombre));

        Id = Guid.NewGuid();
        ProyectoId = proyectoId;
        Nombre = nombre.Trim();
        Orden = orden;
    }

    internal void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la columna es obligatorio.", nameof(nombre));

        Nombre = nombre.Trim();
    }

    internal void CambiarOrden(decimal nuevoOrden) => Orden = nuevoOrden;
}
