using FluentAssertions;
using FluentValidation;
using Funkos.Dto.Categoria;
using Funkos.Repositories.Categoria;
using Funkos.Validations;
using Moq;

namespace Funkos.Test.Validations;

[TestFixture]
public class CategoriaValidationTest
{
    private Mock<ICategoriaRepository> _mockRepo = null!;
    private CategoriaCreateValidation _createValidator = null!;
    private CategoriaUpdateValidation _updateValidator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<ICategoriaRepository>();

        _createValidator = new CategoriaCreateValidation(_mockRepo.Object);
        _updateValidator = new CategoriaUpdateValidation(_mockRepo.Object);
    }

    [Test]
    public async Task CreateValidator_Falla_SiNombreEstaVacio()
    {
        // Arrange
        var dto = new CategoriaCreateDto { Name = "", Descripcion = "Descripción válida" };

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "El nombre es obligatorio");
    }

    [Test]
    public async Task CreateValidator_Falla_SiNombreYaExiste()
    {
        // Arrange
        var dto = new CategoriaCreateDto { Name = "Anime", Descripcion = "Descripción válida" };

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Anime", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Ya existe una categoria con ese nombre");
    }

    [Test]
    public async Task CreateValidator_Pasa_SiDtoEsValido()
    {
        // Arrange
        var dto = new CategoriaCreateDto { Name = "Marvel", Descripcion = "Figuras de Marvel" };

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Marvel", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task UpdateValidator_Falla_SiNombreDuplicadoExcluyendoIdActual()
    {
        // Arrange
        var dto = new CategoriaUpdateDto { Name = "Anime Duplicado", Descripcion = "Actualizado" };
        var categoriaId = "CAT12345678";

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Anime Duplicado", categoriaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Creamos el contexto manualmente para pasar el RootContextData["CatId"]
        var validationContext = new ValidationContext<CategoriaUpdateDto>(dto);
        validationContext.RootContextData["CatId"] = categoriaId;

        // Act
        var result = await _updateValidator.ValidateAsync(validationContext);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Ya existe una categoria con ese nombre");
    }

    [Test]
    public async Task UpdateValidator_Pasa_SiActualizacionEsValida()
    {
        // Arrange
        var dto = new CategoriaUpdateDto { Name = "Anime Actualizado", Descripcion = "Nueva descripción" };
        var categoriaId = "CAT12345678";

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Anime Actualizado", categoriaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var validationContext = new ValidationContext<CategoriaUpdateDto>(dto);
        validationContext.RootContextData["CatId"] = categoriaId;

        // Act
        var result = await _updateValidator.ValidateAsync(validationContext);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}