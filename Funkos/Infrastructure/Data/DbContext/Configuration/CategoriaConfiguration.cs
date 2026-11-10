using Funkos.idGenerators;
using Funkos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Funkos.DbContext.Configuration;

public class CategoriaConfiguration: IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categoria");
        
        builder.HasKey(p => p.IdY);
        
        // Clave => string generado por la App al añadir la entidad. 
        builder.Property(p => p.IdY)
            .HasMaxLength(IdYoutube.Longitud) // longitud igual a 11 (a no ser q se cambie)
            .ValueGeneratedOnAdd() // ==> obligatorio para claves string: sin él EF Core no invoca al generador y rechaza la entidad por clave nula.
            .HasValueGenerator<YouTubeIdValueGenerator>(); // Seleccionamos nuestro generador
        
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();
        
        builder.Property(p => p.Descripcion).HasMaxLength(300).IsRequired();
        
        builder.HasMany(c => c.Funkos)          // Una categoría tiene muchos Funkos
            .WithOne(f => f.Categoria)           // Un Funko tiene una única Categoria
            .HasForeignKey(f => f.CategoriaId)   // Usando la clave ajena CategoriaId
            .OnDelete(DeleteBehavior.Restrict);         // Evita borrar la categoría si tiene funkos dentro
        
        //Concurrencia
        builder.Property(p => p.Version)
            .IsConcurrencyToken();
    }
    
}