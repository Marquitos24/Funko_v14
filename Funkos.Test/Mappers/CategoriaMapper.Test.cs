using FluentAssertions;
using Funkos.Dto.Categoria;
using Funkos.Mappers;
using Funkos.Models;

namespace Funkos.Test.Mappers;

[TestFixture]
public class CategoriaMapper_Test
{
    [Test]
    public void ToEntityCreate_Devuelve_CategoriaModelMapeadaCorrectamente()
    {
        // Arrange
        var createDto = new CategoriaCreateDto
        {
            Name = "Anime",
            Descripcion = "Figuras de series de anime y manga"
        };

        // Act
        var resultado = createDto.ToEntityCreate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Name.Should().Be("Anime");
        resultado.Descripcion.Should().Be("Figuras de series de anime y manga");
    }

    [Test]
    public void ToEntityUpdate_Devuelve_CategoriaModelActualizadaMapeadaCorrectamente()
    {
        // Arrange
        var updateDto = new CategoriaUpdateDto
        {
            IdY = "CAT12345678",
            Name = "Marvel Updated",
            Descripcion = "Colección actualizada de Marvel"
        };

        // Act
        var resultado = updateDto.ToEntityUpdate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.IdY.Should().Be("CAT12345678");
        resultado.Name.Should().Be("Marvel Updated");
        resultado.Descripcion.Should().Be("Colección actualizada de Marvel");
    }

    [Test]
    public void ToDtoResponse_Devuelve_CategoriaDtoResponseMapeadoCorrectamente()
    {
        // Arrange
        var fechaCreacion = DateTime.UtcNow;
        var categoria = new Categoria
        {
            IdY = "SW123456789",
            Name = "Star Wars",
            Descripcion = "Galactic collectibles",
            CreateAt = fechaCreacion,
            UpdateAt = null
        };

        // Act
        var resultado = categoria.ToDtoResponse();

        // Assert
        resultado.Should().NotBeNull();
        resultado.IdY.Should().Be("SW123456789");
        resultado.Name.Should().Be("Star Wars");
        resultado.Descripcion.Should().Be("Galactic collectibles");
        resultado.CreateAt.Should().Be(fechaCreacion);
        resultado.UpdateAt.Should().BeNull();
    }

    [Test]
    public void ToDtoResponseWithFunkos_Devuelve_CategoriaWithFunkosResponseDtoMapeadoCorrectamente()
    {
        // Arrange
        var fechaCreacion = DateTime.UtcNow;
        var categoria = new Categoria
        {
            IdY = "DC123456789",
            Name = "DC Comics",
            Descripcion = "Superhéroes DC",
            CreateAt = fechaCreacion,
            Funkos = new List<Funko>
            {
                new Funko
                {
                    Id = 1,
                    Name = "Batman",
                    Precio = 15.00m,
                    Stock = 5,
                    CategoriaId = "DC123456789",
                    ProveedorId = "prov12345678901234567890"
                }
            }
        };

        // Act
        var resultado = categoria.ToDtoResponseWithFunkos();

        // Assert
        resultado.Should().NotBeNull();
        resultado.IdY.Should().Be("DC123456789");
        resultado.Name.Should().Be("DC Comics");
        resultado.Funkos.Should().NotBeNull();
        resultado.Funkos.Should().HaveCount(1);
        
        var funkoDto = resultado.Funkos.First();
        funkoDto.Id.Should().Be(1);
        funkoDto.Name.Should().Be("Batman");
        funkoDto.Precio.Should().Be(15.00m);
    }
}