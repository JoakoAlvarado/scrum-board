using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Persistence.Configurations;

public class TareaConfiguration : IEntityTypeConfiguration<Tarea>
{
    public void Configure(EntityTypeBuilder<Tarea> builder)
    {
        builder.ToTable("tareas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Descripcion).HasMaxLength(4000);
        builder.Property(t => t.Prioridad).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Orden).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(t => t.FechaCreacion).IsRequired();

        // Índices que sostienen la estrategia de orden fraccionario y los reportes/SignalR
        // por proyecto (ver docs/decisiones.md).
        builder.HasIndex(t => new { t.ColumnaId, t.Orden });
        builder.HasIndex(t => t.ProyectoId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.ResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
