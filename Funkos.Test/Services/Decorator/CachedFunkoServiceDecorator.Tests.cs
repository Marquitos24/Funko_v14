using FluentAssertions;
using Funkos.Cache;
using Funkos.Dto.Funko;
using Funkos.Errors;
using Funkos.Service;
using Funkos.Service.Decorator;
using Microsoft.Extensions.Logging;
using Moq;

namespace Funkos.Test.Services.Decorator;

[TestFixture]
public class CachedFunkoServiceDecoratorTests
{
    private Mock<IFunkoService> _mockInnerService = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<ILogger<CachedFunkoServiceDecorator>> _mockLogger = null!;
    private CachedFunkoServiceDecorator _decorator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockInnerService = new Mock<IFunkoService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedFunkoServiceDecorator>>();

        _decorator = new CachedFunkoServiceDecorator(
            _mockInnerService.Object, 
            _mockCacheService.Object, 
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_ValorDesdeCacheSiExisteCacheHit()
    {
        // Arrange
        var cachedDto = new FunkosDtoResponse(
            1,
            "Funko Cached",
            15.00m,
            "CAT123",
            "prov12345678901234567890",
            5,
            "default.png",
            DateTime.UtcNow,
            null,
            null
        );

        _mockCacheService
            .Setup(c => c.GetAsync<FunkosDtoResponse>("funko:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        // Act
        var resultado = await _decorator.GetByIdAsync(1, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Funko Cached");

        _mockInnerService.Verify(s => s.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_ValorDesdeServicioYGuardaEnCacheSiHayCacheMiss()
    {
        // Arrange
        var funkoDto = new FunkosDtoResponse(
            1,
            "Funko Fresh",
            20.00m,
            "CAT123",
            "prov12345678901234567890",
            10,
            "default.png",
            DateTime.UtcNow,
            null,
            null
        );

        _mockCacheService
            .Setup(c => c.GetAsync<FunkosDtoResponse>("funko:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FunkosDtoResponse?)null);

        _mockInnerService
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<FunkosDtoResponse, DomainErrors>(funkoDto));

        // Act
        var resultado = await _decorator.GetByIdAsync(1, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Funko Fresh");

        _mockCacheService.Verify(
            c => c.SetAsync("funko:1", funkoDto, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_Devuelve_ResultadoInvalidandoCacheSiEsExitoso()
    {
        // Arrange
        _mockInnerService
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<bool, DomainErrors>(true));

        // Act
        var resultado = await _decorator.DeleteAsync(1, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();

        _mockCacheService.Verify(c => c.RemoveAsync("funko:1", It.IsAny<CancellationToken>()), Times.Once);
    }
}