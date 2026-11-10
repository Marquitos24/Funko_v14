using FluentAssertions;
using Funkos.Cache;
using Funkos.Dto.Proveedor;
using Funkos.Errors;
using Funkos.Service.Decorator;
using Funkos.Service.Proveedor;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;

namespace Funkos.Test.Services;

[TestFixture]
public class CachedProveedorServiceDecoratorTest
{
    private Mock<IProveedorService> _mockInnerService = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<ILogger<CachedProveedorServiceDecorator>> _mockLogger = null!;
    private CachedProveedorServiceDecorator _decorator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockInnerService = new Mock<IProveedorService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedProveedorServiceDecorator>>();

        _decorator = new CachedProveedorServiceDecorator(
            _mockInnerService.Object,
            _mockCacheService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_Devuelve_ListaDesdeCacheSiExisteCacheHit()
    {
        // Arrange
        var cachedList = new List<ProveedorDtoResponse>
        {
            new ProveedorDtoResponse(ObjectId.GenerateNewId().ToString(), "Distribuidor A", "Desc A", DateTime.UtcNow, null, null)
        };

        _mockCacheService
            .Setup(c => c.GetAsync<List<ProveedorDtoResponse>>("proveedor:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        // Act
        var resultado = await _decorator.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);

        _mockInnerService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_Devuelve_ListaDesdeServicioYGuardaEnCacheSiHayCacheMiss()
    {
        // Arrange
        var proveedorList = new List<ProveedorDtoResponse>
        {
            new ProveedorDtoResponse(ObjectId.GenerateNewId().ToString(), "Distribuidor B", "Desc B", DateTime.UtcNow, null, null)
        };

        _mockCacheService
            .Setup(c => c.GetAsync<List<ProveedorDtoResponse>>("proveedor:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ProveedorDtoResponse>?)null);

        _mockInnerService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<IEnumerable<ProveedorDtoResponse>, DomainErrors>(proveedorList));

        // Act
        var resultado = await _decorator.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _mockCacheService.Verify(
            c => c.SetAsync("proveedor:all", proveedorList, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_ProveedorDesdeCacheSiExisteCacheHit()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var proveedorDto = new ProveedorDtoResponse(id.ToString(), "Distribuidor C", "Desc C", DateTime.UtcNow, null, null);

        _mockCacheService
            .Setup(c => c.GetAsync<ProveedorDtoResponse>($"proveedor:{id}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedorDto);

        // Act
        var resultado = await _decorator.GetByIdAsync(id, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Distribuidor C");

        _mockInnerService.Verify(s => s.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddAsync_Devuelve_ResultadoInvalidaCacheGetAllSiEsExitoso()
    {
        // Arrange
        var createDto = new ProveedorCreateDto { Name = "Nuevo", Descripcion = "Desc" };
        var proveedorDto = new ProveedorDtoResponse(ObjectId.GenerateNewId().ToString(), "Nuevo", "Desc", DateTime.UtcNow, null, null);

        _mockInnerService
            .Setup(s => s.AddAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<ProveedorDtoResponse, DomainErrors>(proveedorDto));

        // Act
        var resultado = await _decorator.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _mockCacheService.Verify(c => c.RemoveAsync("proveedor:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_Devuelve_ResultadoInvalidaCacheGetAllYGetByIdSiEsExitoso()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var updateDto = new ProveedorUpdateDto { Id = id, Name = "Actualizado", Descripcion = "Desc" };
        var proveedorDto = new ProveedorDtoResponse(id.ToString(), "Actualizado", "Desc", DateTime.UtcNow, DateTime.UtcNow, null);

        _mockInnerService
            .Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<ProveedorDtoResponse, DomainErrors>(proveedorDto));

        // Act
        var resultado = await _decorator.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _mockCacheService.Verify(c => c.RemoveAsync("proveedor:all", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync($"proveedor:{id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_Devuelve_ResultadoInvalidaCacheSiEsExitoso()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();

        _mockInnerService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<bool, DomainErrors>(true));

        // Act
        var resultado = await _decorator.DeleteAsync(id, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();

        _mockCacheService.Verify(c => c.RemoveAsync("proveedor:all", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync($"proveedor:{id}", It.IsAny<CancellationToken>()), Times.Once);
    }
}