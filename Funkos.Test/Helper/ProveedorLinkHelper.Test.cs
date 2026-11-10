using FluentAssertions;
using Funkos.Helper;
using MongoDB.Bson;

namespace Funkos.Test.Helper;

[TestFixture]
public class ProveedorLinkHelperTest
{
    [Test]
    public void GenerateLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        var id = ObjectId.GenerateNewId().ToString();
        string baseUrl = "https://api.funkos.com/api/proveedores";

        // Act
        var resultado = ProveedorLinkHelper.GenerateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Actualizar\"; method=\"PUT\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Eliminar\"; method=\"DELETE\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
    }

    [Test]
    public void GenerateUpdateLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        var id = ObjectId.GenerateNewId().ToString();
        string baseUrl = "https://api.funkos.com/api/proveedores";

        // Act
        var resultado = ProveedorLinkHelper.GenerateUpdateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Eliminar\"; method=\"DELETE\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
    }

    [Test]
    public void GenerateDeleteLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        var id = ObjectId.GenerateNewId().ToString();
        string baseUrl = "https://api.funkos.com/api/proveedores";

        // Act
        var resultado = ProveedorLinkHelper.GenerateDeleteLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Crear nuevo\"; method=\"POST\"");
    }
}