using System.Text.Json;
using FluentAssertions;
using Funkos.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Funkos.Test.Services.Cache;

[TestFixture]
public class RedisCacheServiceTest
{
    private Mock<IDistributedCache> _mockDistributedCache = null!;
    private Mock<IConnectionMultiplexer> _mockMultiplexer = null!;
    private Mock<IDatabase> _mockDatabase = null!;
    private IOptions<CacheOptions> _options = null!;
    
    private RedisCacheService _cacheService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();

        _mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

        _options = Options.Create(new CacheOptions
        {
            DefaultExpirationMinutes = 30,
            SlidingExpirationMinutes = 10
        });

        _cacheService = new RedisCacheService(
            _mockDistributedCache.Object,
            _options,
            _mockMultiplexer.Object
        );
    }

    [Test]
    public async Task GetAsync_Devuelve_ObjetoDeserializadoSiExiste()
    {
        // Arrange
        var testObj = new { Name = "Test Funko", Price = 15.5m };
        var jsonString = JsonSerializer.Serialize(testObj);

        _mockDistributedCache
            .Setup(c => c.GetStringAsync("funko:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonString);

        // Act
        var resultado = await _cacheService.GetAsync<dynamic>("funko:1", CancellationToken.None);

        // Assert
        resultado.Should().NotBeNull();
        _mockDistributedCache.Verify(c => c.GetStringAsync("funko:1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAsync_Devuelve_DefaultSiNoExiste()
    {
        // Arrange
        _mockDistributedCache
            .Setup(c => c.GetStringAsync("funko:inexistente", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var resultado = await _cacheService.GetAsync<string>("funko:inexistente", CancellationToken.None);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_SerializaYGuardaEnCacheCorrectamente()
    {
        // Arrange
        var testObj = new { Name = "Funko Pop" };

        _mockDistributedCache
            .Setup(c => c.SetStringAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync("key1", testObj, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetStringAsync("key1", It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task RemoveAsync_EliminaClaveDeCache()
    {
        // Arrange
        _mockDistributedCache
            .Setup(c => c.RemoveAsync("key1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.RemoveAsync("key1", CancellationToken.None);

        // Assert
        _mockDistributedCache.Verify(c => c.RemoveAsync("key1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExistsAsync_Devuelve_TrueSiExisteYFalseSiNo()
    {
        // Arrange
        _mockDistributedCache
            .Setup(c => c.GetStringAsync("key_existe", It.IsAny<CancellationToken>()))
            .ReturnsAsync("data");

        _mockDistributedCache
            .Setup(c => c.GetStringAsync("key_no_existe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var existe = await _cacheService.ExistsAsync("key_existe", CancellationToken.None);
        var noExiste = await _cacheService.ExistsAsync("key_no_existe", CancellationToken.None);

        // Assert
        existe.Should().BeTrue();
        noExiste.Should().BeFalse();
    }

    [Test]
    public async Task GetOrSetAsync_Devuelve_DatoDirectamenteSiExisteCacheHit()
    {
        // Arrange
        var jsonString = JsonSerializer.Serialize("ValorEnCache");
        _mockDistributedCache
            .Setup(c => c.GetStringAsync("mi_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonString);

        bool factoryEjecutada = false;

        // Act
        var resultado = await _cacheService.GetOrSetAsync("mi_key", async () =>
        {
            factoryEjecutada = true;
            return await Task.FromResult("ValorNuevo");
        }, null, CancellationToken.None);

        // Assert
        resultado.Should().Be("ValorEnCache");
        factoryEjecutada.Should().BeFalse(); // La factoría de base de datos nunca debe llamarse en un cache hit
    }

    [Test]
    public async Task GetOrSetAsync_EjecutaFactoryYGuardaSiHayCacheMiss()
    {
        // Arrange
        _mockDistributedCache
            .Setup(c => c.GetStringAsync("mi_key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Simulamos que el lock distribuido se consigue con éxito (StringSetAsync devuelve true)
        _mockDatabase
            .Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<TimeSpan?>(), 
                It.IsAny<bool>(), 
                It.IsAny<When>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var resultado = await _cacheService.GetOrSetAsync("mi_key", async () =>
        {
            return await Task.FromResult("DatoDesdeDb");
        }, null, CancellationToken.None);

        // Assert
        resultado.Should().Be("DatoDesdeDb");
        
        // Comprobamos que se intentó guardar en la caché tras el miss
        _mockDistributedCache.Verify(
            c => c.SetStringAsync("mi_key", It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}