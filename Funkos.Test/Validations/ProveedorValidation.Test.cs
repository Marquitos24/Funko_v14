using FluentAssertions;
using FluentValidation;
using Funkos.Dto.Proveedor;
using Funkos.Repositories.Proveedor;
using Funkos.Validations;
using MongoDB.Bson;
using Moq;

namespace Funkos.Test.Validations;

[TestFixture]
public class ProveedorValidationTest
{
    private Mock<IProveedorRepository> _mockRepo = null!;
    private ProveedorCreateValidation _createValidator = null!;
    private ProveedorUpdateValidation _updateValidator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IProveedorRepository>();

        _createValidator = new ProveedorCreateValidation(_mockRepo.Object);
        _updateValidator = new ProveedorUpdateValidation(_mockRepo.Object);
    }

    [Test]
    public async Task CreateValidator_Falla_SiNombreEstaVacio()
    {
        // Arrange
        var dto = new ProveedorCreateDto { Name = "", Descripcion = "Descripción válida" };

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
        var dto = new ProveedorCreateDto { Name = "Distribuidor Express", Descripcion = "Descripción válida" };

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Distribuidor Express", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Ya existe un proveedor con ese nombre");
    }

    [Test]
    public async Task CreateValidator_Pasa_SiDtoEsValido()
    {
        // Arrange
        var dto = new ProveedorCreateDto { Name = "Nuevo Proveedor", Descripcion = "Servicio de distribución" };

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Nuevo Proveedor", null, It.IsAny<CancellationToken>()))
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
        var dto = new ProveedorUpdateDto { Name = "Proveedor Duplicado", Descripcion = "Actualizado" };
        var proveedorId = ObjectId.GenerateNewId();

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Proveedor Duplicado", proveedorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Creamos el contexto manualmente para pasar el RootContextData["ProvId"] como ObjectId
        var validationContext = new ValidationContext<ProveedorUpdateDto>(dto);
        validationContext.RootContextData["ProvId"] = proveedorId;

        // Act
        var result = await _updateValidator.ValidateAsync(validationContext);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Ya existe un proveedor con ese nombre");
    }

    [Test]
    public async Task UpdateValidator_Pasa_SiActualizacionEsValida()
    {
        // Arrange
        var dto = new ProveedorUpdateDto { Name = "Proveedor Actualizado", Descripcion = "Descripción al día" };
        var proveedorId = ObjectId.GenerateNewId();

        _mockRepo
            .Setup(r => r.ExistsByNameAsync("Proveedor Actualizado", proveedorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var validationContext = new ValidationContext<ProveedorUpdateDto>(dto);
        validationContext.RootContextData["ProvId"] = proveedorId;

        // Act
        var result = await _updateValidator.ValidateAsync(validationContext);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}