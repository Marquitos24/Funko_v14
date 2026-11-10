using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Funkos.Validations;

namespace Funkos.Test.Validations;

[TestFixture]
public class NoLinkAttributeTest
{
    private NoLinkAttribute _attribute = null!;

    [SetUp]
    public void SetUp()
    {
        _attribute = new NoLinkAttribute();
    }

    [Test]
    public void IsValid_Devuelve_SuccessSiElValorEsNulo()
    {
        // Act
        var resultado = _attribute.GetValidationResult(null, new ValidationContext(new object()));

        // Assert
        resultado.Should().Be(ValidationResult.Success);
    }

    [Test]
    public void IsValid_Devuelve_SuccessSiElTextoNoContieneEnlaces()
    {
        // Arrange
        var textoValido = "Este es un texto completamente normal sin enlaces.";

        // Act
        var resultado = _attribute.GetValidationResult(textoValido, new ValidationContext(new object()));

        // Assert
        resultado.Should().Be(ValidationResult.Success);
    }

    [TestCase("Visita https://google.com para más info")]
    [TestCase("Mira nuestra web www.miweb.es")]
    [TestCase("Contáctanos en test@dominio.io o visita dominio.io")]
    public void IsValid_Devuelve_ErrorSiContieneEnlace(string textoConEnlace)
    {
        // Act
        var resultado = _attribute.GetValidationResult(textoConEnlace, new ValidationContext(new object()));

        // Assert
        resultado.Should().NotBe(ValidationResult.Success);
        resultado!.ErrorMessage.Should().Be("No se admiten enlaces");
    }
}