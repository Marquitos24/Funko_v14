using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Funkos.Dto;
using Funkos.Dto.Funko;
using Funkos.Repositories;
using Funkos.Repositories.Categoria;
using Funkos.Repositories.Proveedor;
using Funkos.Validations;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Funkos.Test.Validations;

[TestFixture]
public class FunkoValidationTest
{
    private Mock<ICategoriaRepository> _mockCategoriaRepo = null!;
    private Mock<IProveedorRepository> _mockProveedorRepo = null!;
    private Mock<IFunkoRepository> _mockFunkoRepo = null!;

    private CreateFunkoValidationAsync _createValidator = null!;
    private UpdateFunkoValidation _updateValidator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockCategoriaRepo = new Mock<ICategoriaRepository>();
        _mockProveedorRepo = new Mock<IProveedorRepository>();
        _mockFunkoRepo = new Mock<IFunkoRepository>();

        _createValidator = new CreateFunkoValidationAsync(
            _mockCategoriaRepo.Object,
            _mockProveedorRepo.Object,
            _mockFunkoRepo.Object
        );

        _updateValidator = new UpdateFunkoValidation(
            _mockCategoriaRepo.Object,
            _mockProveedorRepo.Object,
            _mockFunkoRepo.Object
        );
    }

    [Test]
    public async Task CreateValidator_Falla_SiPrecioEsMenorAUno()
    {
        // Arrange
        var dto = new FunkoCreateDto
        {
            Name = "Funko Test",
            Precio = 0.50m,
            Stock = 5,
            CategoriaId = "CAT12345678",
            ProveedorId = "prov12345678901234567890"
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Precio);
    }

    [Test]
    public async Task CreateValidator_Falla_SiNombreYaExiste()
    {
        // Arrange
        var dto = new FunkoCreateDto
        {
            Name = "Funko Existente",
            Precio = 15.00m,
            Stock = 5,
            CategoriaId = "CAT12345678",
            ProveedorId = "prov12345678901234567890"
        };

        _mockFunkoRepo
            .Setup(r => r.ExistsByNameAsync("Funko Existente", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("El nombre del funko ya existe");
    }

    [Test]
    public async Task UpdateValidator_Falla_SiNombreDuplicadoExcluyendoIdActual()
    {
        // Arrange
        var dto = new FunkoUpdateDto
        {
            Name = "Funko Duplicado",
            Precio = 15.00m,
            Stock = 5,
            CategoriaId = "CAT12345678",
            ProveedorId = "prov12345678901234567890"
        };

        _mockFunkoRepo
            .Setup(r => r.ExistsByNameAsync("Funko Duplicado", 10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Creamos el contexto manualmente e inyectamos el ID en RootContextData
        var validationContext = new ValidationContext<FunkoUpdateDto>(dto);
        validationContext.RootContextData["FunkoId"] = 10L;

        // Act
        var result = await _updateValidator.ValidateAsync(validationContext);

        // Assert (Usando FluentAssertions puro)
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "El nombre del funko ya existe");
    }

    [Test]
    public async Task CreateValidator_Falla_SiExtensionDeImagenNoEsValida()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("documento.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        var dto = new FunkoCreateDto
        {
            Name = "Funko Imagen Invalida",
            Precio = 15.00m,
            Stock = 5,
            CategoriaId = "CAT12345678",
            ProveedorId = "prov12345678901234567890",
            Image = mockFile.Object
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Image!.FileName);
    }
}