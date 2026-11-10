using FluentAssertions;
using Funkos.Cache;
using Funkos.Dto.Categoria;
using Funkos.Errors;
using Funkos.Service.Categoria;
using Funkos.Service.Decorator;
using Microsoft.Extensions.Logging;
using Moq;

namespace Funkos.Test.Services.Decorator;

[TestFixture]
public class CachedCategoriaServiceDecoratorTest
{
    private Mock<ICategoriaService> _mockInnerService = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<ILogger<CachedCategoriaServiceDecorator>> _mockLogger = null!;
    private CachedCategoriaServiceDecorator _decorator = null!;

    
    [SetUp]
    public void SetUp()
    {
        _mockInnerService = new Mock<ICategoriaService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedCategoriaServiceDecorator>>();

        _decorator = new CachedCategoriaServiceDecorator(
            _mockInnerService.Object,
            _mockCacheService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_Devuelve_ListaDesdeCacheSiExisteCacheHit()
    {
        // Arrange
        var cachedList = new List<CategoriaDtoResponse>
        {
            new CategoriaDtoResponse("CAT12345678", "Anime", "Figuras de anime", DateTime.UtcNow, null)
        };

        _mockCacheService
            .Setup(c => c.GetAsync<List<CategoriaDtoResponse>>("categoria:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        // Act
        var resultado = await _decorator.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);
        resultado.Value[0].Name.Should().Be("Anime");

        // Comprobamos que el servicio interno nunca fue llamado debido al Hit
        _mockInnerService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_Devuelve_ListaDesdeServicioYGuardaEnCacheSiHayCacheMiss()
    {
        // Arrange
        var categoriasList = new List<CategoriaDtoResponse>
        {
            new CategoriaDtoResponse("CAT12345678", "Marvel", "Superhéroes", DateTime.UtcNow, null)
        };

        _mockCacheService
            .Setup(c => c.GetAsync<List<CategoriaDtoResponse>>("categoria:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CategoriaDtoResponse>?)null);

        _mockInnerService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<List<CategoriaDtoResponse>, DomainErrors>(categoriasList));

        // Act
        var resultado = await _decorator.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);

        // Comprobamos que se guardó en caché tras el Miss
        _mockCacheService.Verify(
            c => c.SetAsync("categoria:all", categoriasList, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), 
            Times.Once
        );
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_CategoriaDesdeCacheSiExisteCacheHit()
    {
        // Arrange
        var categoriaDto = new CategoriaWithFunkosResponseDto(
            "CAT12345678",
            "Star Wars",
            "Galactic",
            new List<Dto.Funko.FunkoParaCategoriaDto>(),
            DateTime.UtcNow,
            null
        );

        _mockCacheService
            .Setup(c => c.GetAsync<CategoriaWithFunkosResponseDto>("categoria:CAT12345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoriaDto);

        // Act
        var resultado = await _decorator.GetByIdAsync("CAT12345678", CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Star Wars");

        _mockInnerService.Verify(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddAsync_Devuelve_ResultadoInvalidaCacheGetAllSiEsExitoso()
    {
        // Arrange
        var createDto = new CategoriaCreateDto { Name = "Terror", Descripcion = "Películas de terror" };
        var categoriaDto = new CategoriaDtoResponse("CAT99999999", "Terror", "Películas de terror", DateTime.UtcNow, null);

        _mockInnerService
            .Setup(s => s.AddAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<CategoriaDtoResponse, DomainErrors>(categoriaDto));

        // Act
        var resultado = await _decorator.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        
        // Comprobamos que se invalidó el listado global en caché
        _mockCacheService.Verify(c => c.RemoveAsync("categoria:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_Devuelve_ResultadoInvalidaCacheGetAllYGetByIdSiEsExitoso()
    {
        // Arrange
        var updateDto = new CategoriaUpdateDto { Name = "Terror Updated", Descripcion = "Actualizado" };
        var categoriaDto = new CategoriaDtoResponse("CAT12345678", "Terror Updated", "Actualizado", DateTime.UtcNow, DateTime.UtcNow);

        _mockInnerService
            .Setup(s => s.UpdateAsync("CAT12345678", updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<CategoriaDtoResponse, DomainErrors>(categoriaDto));

        // Act
        var resultado = await _decorator.UpdateAsync("CAT12345678", updateDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        // Comprobamos que se eliminó tanto la lista global como la clave específica del ID
        _mockCacheService.Verify(c => c.RemoveAsync("categoria:all", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync("categoria:CAT12345678", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_Devuelve_ResultadoInvalidaCacheSiEsExitoso()
    {
        // Arrange
        _mockInnerService
            .Setup(s => s.DeleteAsync("CAT12345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<bool, DomainErrors>(true));

        // Act
        var resultado = await _decorator.DeleteAsync("CAT12345678", CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();

        // Comprobamos la invalidación de ambas cachés
        _mockCacheService.Verify(c => c.RemoveAsync("categoria:all", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync("categoria:CAT12345678", It.IsAny<CancellationToken>()), Times.Once);
    }
}