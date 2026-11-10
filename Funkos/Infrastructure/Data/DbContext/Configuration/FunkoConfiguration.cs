using Funkos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Funkos.DbContext.Configuration;

public class FunkoConfiguration : IEntityTypeConfiguration<Funko>
{
    public void Configure(EntityTypeBuilder<Funko> builder)
    {
        builder.ToTable("Funkos");
        
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd(); 
        
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();
        
        builder.Property(p => p.Precio).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Stock).HasDefaultValue(0);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Funko_Precio", "\"Precio\" >= 1.00");
            t.HasCheckConstraint("CK_Funko_Stock", "\"Stock\" >= 0");
        });
        
        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Funkos)
            .HasForeignKey(p => p.CategoriaId);
        
        builder.Property(p => p.CategoriaId)
            .HasMaxLength(11).IsRequired();
        
        builder.Property(p => p.ProveedorId)
            .HasMaxLength(24).IsRequired();
        
        // Para no borrar los datos de un usurio en laBD ponemos un filtro para que si se elimina
        // los descarte de las otras consultas (en un filtro)
        builder.HasQueryFilter(p => !p.IsDeleted);
        
        //Para concurrencia
        builder.Property(p => p.Version)
            .IsConcurrencyToken();
    }
}