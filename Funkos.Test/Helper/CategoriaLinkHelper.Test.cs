using FluentAssertions;
using Funkos.Helper;

namespace Funkos.Test.Helper;

[TestFixture]
public class CategoriaLinkHelperTest
{
    [Test]
    public void GenerateLinks_Devuelve_CadenaDeEnlacesHateoasCorrectaConIdString()
    {
        // Arrange
        string id = "CAT12345";
        string baseUrl = "https://api.funkos.com/api/categorias";

        // Act
        var resultado = CategoriaLinkHelper.GenerateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Actualizar\"; method=\"PUT\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Eliminar\"; method=\"DELETE\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
    }

    [Test]
    public void GenerateUpdateLinks_Devuelve_CadenaDeEnlacesHateoasCorrectaConIdString()
    {
        // Arrange
        string id = "CAT12345";
        string baseUrl = "https://api.funkos.com/api/categorias";

        // Act
        var resultado = CategoriaLinkHelper.GenerateUpdateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Eliminar\"; method=\"DELETE\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
    }

    [Test]
    public void GenerateDeleteLinks_Devuelve_CadenaDeEnlacesHateoasCorrectaConIdString()
    {
        // Arrange
        string id = "CAT12345";
        string baseUrl = "https://api.funkos.com/api/categorias";

        // Act
        var resultado = CategoriaLinkHelper.GenerateDeleteLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"Comprobar\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Lista\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"Crear nuevo\"; method=\"POST\"");
    }
}