using ScrumBoard.Domain.Entities.Enums;
using ScrumBoard.Domain.Exceptions;

namespace ScrumBoard.Domain.Entities;

public class Proyecto
{
    private readonly List<Columna> _columnas = new();
    private readonly List<Tarea> _tareas = new();

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Descripcion { get; private set; } = string.Empty;
    public DateTime FechaInicio { get; private set; }
    public DateTime FechaFinPrevista { get; private set; }
    public EstadoProyecto Estado { get; private set; }

    public IReadOnlyCollection<Columna> Columnas => _columnas.AsReadOnly();
    public IReadOnlyCollection<Tarea> Tareas => _tareas.AsReadOnly();

    private Proyecto() { } // EF Core

    public Proyecto(string nombre, string descripcion, DateTime fechaInicio, DateTime fechaFinPrevista)
    {
        ValidarFechas(fechaInicio, fechaFinPrevista);

        Id = Guid.NewGuid();
        Estado = EstadoProyecto.Planificado;
        Actualizar(nombre, descripcion, fechaInicio, fechaFinPrevista);
    }

    public void Actualizar(string nombre, string descripcion, DateTime fechaInicio, DateTime fechaFinPrevista)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proyecto es obligatorio.", nameof(nombre));

        ValidarFechas(fechaInicio, fechaFinPrevista);

        Nombre = nombre.Trim();
        Descripcion = descripcion?.Trim() ?? string.Empty;
        FechaInicio = fechaInicio;
        FechaFinPrevista = fechaFinPrevista;
    }

    public void CambiarEstado(EstadoProyecto nuevoEstado) => Estado = nuevoEstado;

    // --- Columnas: el proyecto es el agregado raíz que garantiza las invariantes ---

    public Columna AgregarColumna(string nombre, decimal orden)
    {
        var columna = new Columna(Id, nombre, orden);
        _columnas.Add(columna);
        return columna;
    }

    public void EliminarColumna(Guid columnaId)
    {
        if (_tareas.Any(t => t.ColumnaId == columnaId))
            throw new DomainException("No se puede eliminar una columna que contiene tareas.");

        var columna = _columnas.FirstOrDefault(c => c.Id == columnaId)
            ?? throw new DomainException("La columna no pertenece a este proyecto.");

        _columnas.Remove(columna);
    }

    // --- Tareas: se valida siempre que la columna pertenezca a este mismo proyecto,
    // manteniendo consistente el campo denormalizado Tarea.ProyectoId ---

    public Tarea AgregarTarea(Guid columnaId, string titulo, string descripcion, Prioridad prioridad, Guid responsableId, decimal orden)
    {
        if (_columnas.All(c => c.Id != columnaId))
            throw new DomainException("La columna indicada no pertenece a este proyecto.");

        var tarea = new Tarea(Id, columnaId, titulo, descripcion, prioridad, responsableId, orden);
        _tareas.Add(tarea);
        return tarea;
    }

    /// <summary>
    /// Mueve una tarea a otra columna (o la reordena dentro de la misma).
    /// <paramref name="nuevoOrden"/> se calcula previamente con
    /// <see cref="Services.CalculadorDeOrden.CalcularOrden"/> a partir de los vecinos
    /// en la columna destino.
    /// </summary>
    public void MoverTarea(Guid tareaId, Guid columnaDestinoId, decimal nuevoOrden)
    {
        if (_columnas.All(c => c.Id != columnaDestinoId))
            throw new DomainException("La columna destino no pertenece a este proyecto.");

        var tarea = _tareas.FirstOrDefault(t => t.Id == tareaId)
            ?? throw new DomainException("La tarea no pertenece a este proyecto.");

        tarea.MoverA(columnaDestinoId, nuevoOrden);
    }

    private static void ValidarFechas(DateTime fechaInicio, DateTime fechaFinPrevista)
    {
        if (fechaFinPrevista < fechaInicio)
            throw new ArgumentException("La fecha de fin prevista no puede ser anterior a la fecha de inicio.");
    }
}
