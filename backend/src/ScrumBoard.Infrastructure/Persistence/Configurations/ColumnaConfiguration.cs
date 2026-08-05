using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Persistence.Configurations;

public class ColumnaConfiguration : IEntityTypeConfiguration<Columna>
{
    public void Configure(EntityTypeBuilder<Columna> builder)
    {
        builder.ToTable("columnas");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Orden).HasColumnType("numeric(18,6)").IsRequired();

        builder.HasIndex(c => new { c.ProyectoId, c.Orden });
    }
}
