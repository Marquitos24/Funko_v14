using Funkos.Mappers;
using Funkos.Models;
using Funkos.Dto;
using NUnit.Framework;
using FluentAssertions;
using Funkos.Dto.Funko;

namespace Funkos.Test.Mappers;

[TestFixture]
public class FunkoMapperTest
{
    [Test]
    public void ToEntityCreate_Devuelve_FunkoModelMapeadoCorrectamente()
    {
        // Arrange
        var createDto = new FunkoCreateDto
        {
            Name = "Iron Man Funko",
            Precio = 25.50m,
            CategoriaId = "ABC12345678",
            ProveedorId = "prov12345678901234567890",
            Stock = 10
        };

        // Act
        var resultado = createDto.ToEntityCreate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Name.Should().Be("Iron Man Funko");
        resultado.Precio.Should().Be(25.50m);
        resultado.CategoriaId.Should().Be("ABC12345678");
        resultado.ProveedorId.Should().Be("prov12345678901234567890");
        resultado.Stock.Should().Be(10);
    }

    [Test]
    public void ToEntityUpdate_Devuelve_FunkoModelActualizadoMapeadoCorrectamente()
    {
        // Arrange
        var updateDto = new FunkoUpdateDto
        {
            Id = 5,
            Name = "Spider-Man Updated",
            Precio = 19.99m,
            CategoriaId = "XYZ87654321",
            ProveedorId = "prov09876543210987654321",
            Stock = 4
        };

        // Act
        var resultado = updateDto.ToEntityUpdate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(5);
        resultado.Name.Should().Be("Spider-Man Updated");
        resultado.Precio.Should().Be(19.99m);
        resultado.CategoriaId.Should().Be("XYZ87654321");
        resultado.ProveedorId.Should().Be("prov09876543210987654321");
        resultado.Stock.Should().Be(4);
    }

    [Test]
    public void ToDtoResponse_Devuelve_FunkosDtoResponseMapeadoCorrectamente()
    {
        // Arrange
        var fechaCreacion = DateTime.UtcNow;
        var funko = new Funko
        {
            Id = 1,
            Name = "Darth Vader",
            Precio = 30.00m,
            CategoriaId = "SW123456789",
            ProveedorId = "prov12345678901234567890",
            Stock = 3,
            Imagen = "vader.png",
            CreateAt = fechaCreacion,
            IsDeleted = false
        };

        // Act
        var resultado = funko.ToDtoResponse();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(1);
        resultado.Name.Should().Be("Darth Vader");
        resultado.Precio.Should().Be(30.00m);
        resultado.CategoriaId.Should().Be("SW123456789");
        resultado.ProveedorId.Should().Be("prov12345678901234567890");
        resultado.Stock.Should().Be(3);
        resultado.image.Should().Be("vader.png");
        resultado.CreateAt.Should().Be(fechaCreacion);
    }
}