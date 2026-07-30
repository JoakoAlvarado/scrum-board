using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Persistence.Configurations;

public class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("proyectos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Descripcion).HasMaxLength(2000);
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(30);

        // El agregado expone las colecciones como IReadOnlyCollection respaldadas
        // por campos privados _columnas/_tareas — EF Core accede a ellos vía backing field.
        builder.Metadata.FindNavigation(nameof(Proyecto.Columnas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Proyecto.Tareas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Columnas)
            .WithOne()
            .HasForeignKey(c => c.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tareas)
            .WithOne()
            .HasForeignKey(t => t.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Nombre);
    }
}
