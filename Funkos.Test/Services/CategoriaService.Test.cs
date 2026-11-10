using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Funkos.Dto.Categoria;
using Funkos.Models;
using Funkos.Repositories.Categoria;
using Funkos.Service.Categoria;
using Microsoft.Extensions.Logging;
using Moq;

namespace Funkos.Test.Services;

[TestFixture]
public class CategoriaServiceTest
{
    private Mock<ICategoriaRepository> _mockRepository = null!;
    private Mock<IValidator<CategoriaCreateDto>> _mockValidatorCreate = null!;
    private Mock<IValidator<CategoriaUpdateDto>> _mockValidatorUpdate = null!;
    private Mock<ILogger<CategoriaService>> _mockLogger = null!;
    
    private CategoriaService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _mockValidatorCreate = new Mock<IValidator<CategoriaCreateDto>>();
        _mockValidatorUpdate = new Mock<IValidator<CategoriaUpdateDto>>();
        _mockLogger = new Mock<ILogger<CategoriaService>>();

        _service = new CategoriaService(
            _mockRepository.Object,
            _mockValidatorCreate.Object,
            _mockValidatorUpdate.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_Devuelve_SuccessConListaDeCategorias()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new Categoria { IdY = "CAT1", Name = "Anime", Descripcion = "Figuras anime", CreateAt = DateTime.UtcNow }
        };

        _mockRepository
            .Setup(r => r.FindAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorias);

        // Act
        var resultado = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);
        resultado.Value[0].Name.Should().Be("Anime");
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_SuccessSiCategoriaExiste()
    {
        // Arrange
        var categoria = new Categoria
        {
            IdY = "CAT1",
            Name = "Marvel",
            Descripcion = "Superhéroes",
            CreateAt = DateTime.UtcNow,
            Funkos = new List<Funko>()
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync("CAT1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoria);

        // Act
        var resultado = await _service.GetByIdAsync("CAT1", CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Marvel");
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_FailureSiIdEsNuloOVacio()
    {
        // Act
        var resultado = await _service.GetByIdAsync("", CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_FailureSiNoExiste()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindByIdAsync("NO_EXISTE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Categoria?)null);

        // Act
        var resultado = await _service.GetByIdAsync("NO_EXISTE", CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddAsync_Devuelve_FailureSiValidacionFalla()
    {
        // Arrange
        var createDto = new CategoriaCreateDto { Name = "", Descripcion = "" };

        _mockValidatorCreate
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "El nombre es obligatorio") }));

        // Act
        var resultado = await _service.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddAsync_Devuelve_FailureSiNombreYaExiste()
    {
        // Arrange
        var createDto = new CategoriaCreateDto { Name = "Anime", Descripcion = "Desc" };

        _mockValidatorCreate
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockRepository
            .Setup(r => r.ExistsByNameAsync("Anime", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var resultado = await _service.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddAsync_Devuelve_SuccessAlCrearCorrectamente()
    {
        // Arrange
        var createDto = new CategoriaCreateDto { Name = "Anime", Descripcion = "Desc" };

        _mockValidatorCreate
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockRepository
            .Setup(r => r.ExistsByNameAsync("Anime", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Categoria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Categoria c, CancellationToken _) =>
            {
                c.IdY = "CAT_NEW";
                return c;
            });

        // Act
        var resultado = await _service.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Anime");
    }

    [Test]
    public async Task DeleteAsync_Devuelve_FailureSiTieneFunkosAsociados()
    {
        // Arrange
        var categoriaConFunkos = new Categoria
        {
            IdY = "CAT1",
            Name = "Anime",
            Funkos = new List<Funko> { new Funko { Id = 1, Name = "Goku", Precio = 10m, Stock = 2, CategoriaId = "c1", ProveedorId = "p1" } },
            Descripcion = "descripcion"
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync("CAT1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoriaConFunkos);

        // Act
        var resultado = await _service.DeleteAsync("CAT1", CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_SuccessAlEliminarCorrectamente()
    {
        // Arrange
        var categoriaLimpia = new Categoria
        {
            IdY = "CAT1",
            Name = "Anime",
            Funkos = new List<Funko>(),
            Descripcion = "descripcion"
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync("CAT1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoriaLimpia);

        _mockRepository
            .Setup(r => r.DeleteAsync("CAT1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync("CAT1", CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }
}