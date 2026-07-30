using Microsoft.EntityFrameworkCore;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Persistence;

public class ScrumBoardDbContext : DbContext
{
    public ScrumBoardDbContext(DbContextOptions<ScrumBoardDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Columna> Columnas => Set<Columna>();
    public DbSet<Tarea> Tareas => Set<Tarea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScrumBoardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
