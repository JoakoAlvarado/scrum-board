using ScrumBoard.Domain.Entities.Enums;

namespace ScrumBoard.Domain.Entities;

public class Tarea
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Descripcion { get; private set; } = string.Empty;
    public Prioridad Prioridad { get; private set; }
    public Guid ResponsableId { get; private set; }
    public Guid ColumnaId { get; private set; }

    /// <summary>
    /// Denormalizado a propósito respecto de Columna.ProyectoId: simplifica
    /// autorización, reportes y agrupación de SignalR sin necesidad de JOIN.
    /// La consistencia se garantiza en Proyecto.MoverTarea/AgregarTarea, que
    /// nunca permiten asignar una columna de otro proyecto.
    /// </summary>
    public Guid ProyectoId { get; private set; }

    public decimal Orden { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private Tarea() { } // EF Core

    internal Tarea(Guid proyectoId, Guid columnaId, string titulo, string descripcion,
        Prioridad prioridad, Guid responsableId, decimal orden)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título de la tarea es obligatorio.", nameof(titulo));

        Id = Guid.NewGuid();
        ProyectoId = proyectoId;
        ColumnaId = columnaId;
        Titulo = titulo.Trim();
        Descripcion = descripcion?.Trim() ?? string.Empty;
        Prioridad = prioridad;
        ResponsableId = responsableId;
        Orden = orden;
        FechaCreacion = DateTime.UtcNow;
    }

    public void Editar(string titulo, string descripcion, Prioridad prioridad, Guid responsableId)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título de la tarea es obligatorio.", nameof(titulo));

        Titulo = titulo.Trim();
        Descripcion = descripcion?.Trim() ?? string.Empty;
        Prioridad = prioridad;
        ResponsableId = responsableId;
    }

    /// <summary>
    /// Cambia de columna (o reordena dentro de la misma). La validación de que
    /// la columna destino pertenezca al mismo proyecto vive en el agregado
    /// <see cref="Proyecto"/>, no acá: esta entidad no conoce las demás columnas.
    /// </summary>
    internal void MoverA(Guid columnaDestinoId, decimal nuevoOrden)
    {
        ColumnaId = columnaDestinoId;
        Orden = nuevoOrden;
    }
}
