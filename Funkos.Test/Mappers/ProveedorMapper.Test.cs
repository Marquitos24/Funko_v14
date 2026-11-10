using FluentAssertions;
using Funkos.Dto.Proveedor;
using Funkos.Mappers;
using Funkos.Models;
using MongoDB.Bson;

namespace Funkos.Test.Mappers;

[TestFixture]
public class ProveedorMapperTest
{
    [Test]
    public void ToEntityCreate_Devuelve_ProveedorModelMapeadoCorrectamente()
    {
    // Arrange
        var createDto = new ProveedorCreateDto
        {
            Name = "Funko Corp",
            Descripcion = "Proveedor principal de figuras coleccionables"
        };

        // Act
        var resultado = createDto.ToEntityCreate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Name.Should().Be("Funko Corp");
        resultado.Descripcion.Should().Be("Proveedor principal de figuras coleccionables");
    }

    [Test]
    public void ToEntityUpdate_Devuelve_ProveedorModelActualizadoMapeadoCorrectamente()
    {
        // Arrange
        var objectId = ObjectId.GenerateNewId();
        var updateDto = new ProveedorUpdateDto
        {
            Id = objectId,
            Name = "Funko Corp Updated",
            Descripcion = "Descripción actualizada del proveedor"
        };

        // Act
        var resultado = updateDto.ToEntityUpdate();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Name.Should().Be("Funko Corp Updated");
        resultado.Descripcion.Should().Be("Descripción actualizada del proveedor");
    }

    [Test]
    public void ToDtoResponse_Devuelve_ProveedorDtoResponseMapeadoCorrectamente()
    {
        // Arrange
        var fechaCreacion = DateTime.UtcNow;
        var objectId = ObjectId.GenerateNewId();
        var proveedor = new Proveedor
        {
            Id = objectId,
            Name = "Distributor S.L.",
            Descripcion = "Distribuidor autorizado",
            CreateAt = fechaCreacion,
            UpdateAt = null,
            DeleteAt = null
        };

        // Act
        var resultado = proveedor.ToDtoResponse();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(objectId.ToString());
        resultado.Name.Should().Be("Distributor S.L.");
        resultado.Descripcion.Should().Be("Distribuidor autorizado");
        resultado.CreateAt.Should().Be(fechaCreacion);
        resultado.UpdateAt.Should().BeNull();
        resultado.DeleteAt.Should().BeNull();
    }
}