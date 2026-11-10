using FluentAssertions;
using Funkos.Helper;

namespace Funkos.Test.Helper;

[TestFixture]
public class LinkHelperTest
{
    [Test]
    public void GenerateLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        long id = 15;
        string baseUrl = "https://api.funkos.com/api/funkos";

        // Act
        var resultado = LinkHelper.GenerateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}>; rel=\"collection\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"selfId\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"update\"; method=\"PUT\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"delete\"; method=\"DELETE\"");
    }

    [Test]
    public void GenerateUpdateLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        long id = 25;
        string baseUrl = "https://api.funkos.com/api/funkos";

        // Act
        var resultado = LinkHelper.GenerateUpdateLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"self\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"delete\"; method=\"DELETE\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"collection\"; method=\"GET\"");
    }

    [Test]
    public void GenerateDeleteLinks_Devuelve_CadenaDeEnlacesHateoasCorrecta()
    {
        // Arrange
        long id = 35;
        string baseUrl = "https://api.funkos.com/api/funkos";

        // Act
        var resultado = LinkHelper.GenerateDeleteLinks(id, baseUrl);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Contain($"<{baseUrl}/{id}>; rel=\"self\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"collection\"; method=\"GET\"");
        resultado.Should().Contain($"<{baseUrl}>; rel=\"create\"; method=\"POST\"");
    }
}