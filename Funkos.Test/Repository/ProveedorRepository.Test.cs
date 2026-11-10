using FluentAssertions;
using Funkos.Models;
using Funkos.Mongo;
using Funkos.Repositories.Proveedor;
using MongoDB.Bson;
using Testcontainers.MongoDb;
using MongoClient = MongoDB.Driver.MongoClient;

namespace Funkos.Test.Repository;

[TestFixture]
public class ProveedorRepositoryTest
{
    private MongoDbContainer _mongoContainer = null!;
    private string _connectionString = null!;
    private MongoDbSettings _settings = null!;
    private MongoDbContext _context = null!;
    private ProveedorRepository _repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        await _mongoContainer.StartAsync();
        _connectionString = _mongoContainer.GetConnectionString();

        _settings = new MongoDbSettings
        {
            ConnectionString = _connectionString,
            DatabaseName = "funkos_proveedor_repo_test_db"
        };
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _mongoContainer.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var client = new MongoClient(_connectionString);
        _context = new MongoDbContext(client, _settings);
        
        // Limpiamos la colección antes de cada test
        await _context.Proveedores.DeleteManyAsync(MongoDB.Driver.Builders<Proveedor>.Filter.Empty);

        _repository = new ProveedorRepository(_context);
    }

    [Test]
    public async Task FindAllAsync_Devuelve_ListaDeProveedoresActivos()
    {
        // Arrange
        await _context.Proveedores.InsertOneAsync(new Proveedor
        {
            Name = "Proveedor Activo 1",
            Descripcion = "Descripción 1",
            IsDeleted = false
        });

        await _context.Proveedores.InsertOneAsync(new Proveedor
        {
            Name = "Proveedor Borrado",
            Descripcion = "Descripción 2",
            IsDeleted = true // No debe aparecer por el filtro de activos
        });

        // Act
        var resultado = await _repository.FindAllAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().ContainSingle();
        resultado.Should().OnlyContain(p => p.Name == "Proveedor Activo 1");
    }

    [Test]
    public async Task FindByIdAsync_Devuelve_ProveedorSiExisteYEstaActivo()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Sony Collectibles",
            Descripcion = "Distribuidor oficial",
            IsDeleted = false
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        // Act
        var resultado = await _repository.FindByIdAsync(proveedor.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Name.Should().Be("Sony Collectibles");
    }

    [Test]
    public async Task FindByIdAsync_Devuelve_NuloSiProveedorEstaBorradoLogicamente()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Proveedor Inactivo",
            Descripcion = "Archivado",
            IsDeleted = true
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        // Act
        var resultado = await _repository.FindByIdAsync(proveedor.Id);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_Devuelve_ProveedorCreadoConFechaYValido()
    {
        // Arrange
        var nuevoProveedor = new Proveedor
        {
            Name = "Funko Europe",
            Descripcion = "Centro logístico europeo"
        };

        // Act
        var resultado = await _repository.CreateAsync(nuevoProveedor);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBe(ObjectId.Empty);
        resultado.Name.Should().Be("Funko Europe");
        resultado.CreateAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_Devuelve_ProveedorModificado()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Proveedor Old",
            Descripcion = "Viejo",
            IsDeleted = false
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        var datosActualizados = new Proveedor
        {
            Id = proveedor.Id,
            Name = "Proveedor Updated",
            Descripcion = "Nuevo",
            IsDeleted = false
        };

        // Act
        var resultado = await _repository.UpdateAsync(proveedor.Id, datosActualizados);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Name.Should().Be("Proveedor Updated");
        resultado.Descripcion.Should().Be("Nuevo");
        resultado.UpdateAt.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_Devuelve_NuloSiProveedorNoExisteOEstaBorrado()
    {
        // Arrange
        var idInexistente = ObjectId.GenerateNewId();
        var datosActualizados = new Proveedor
        {
            Name = "Fantasma",
            Descripcion = "No existe"
        };

        // Act
        var resultado = await _repository.UpdateAsync(idInexistente, datosActualizados);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_TrueYMarcaBorradoLogico()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Proveedor Borrable",
            Descripcion = "Para eliminar",
            IsDeleted = false
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        // Act
        var exito = await _repository.DeleteAsync(proveedor.Id);

        // Assert
        exito.Should().BeTrue();

        // Comprobamos que ya no se encuentra como activo
        var busqueda = await _repository.FindByIdAsync(proveedor.Id);
        busqueda.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_FalseSiProveedorNoExiste()
    {
        // Act
        var exito = await _repository.DeleteAsync(ObjectId.GenerateNewId());

        // Assert
        exito.Should().BeFalse();
    }

    [Test]
    public async Task ExistsAsync_Devuelve_TrueSiIdValidoYExiste()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Proveedor Existente",
            Descripcion = "Activo",
            IsDeleted = false
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        // Act
        var existe = await _repository.ExistsAsync(proveedor.Id.ToString());

        // Assert
        existe.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_Devuelve_FalseSiIdNoEsValidoOInexistente()
    {
        // Act
        var noValido = await _repository.ExistsAsync("id-no-valido-mongo");
        var inexistente = await _repository.ExistsAsync(ObjectId.GenerateNewId().ToString());

        // Assert
        noValido.Should().BeFalse();
        inexistente.Should().BeFalse();
    }

    [Test]
    public async Task ExistsByNameAsync_Devuelve_TrueSiNombreYaExiste()
    {
        // Arrange
        await _context.Proveedores.InsertOneAsync(new Proveedor
        {
            Name = "Distribuciones Norte",
            Descripcion = "Zona norte"
        });

        // Act
        var existe = await _repository.ExistsByNameAsync("Distribuciones Norte");

        // Assert
        existe.Should().BeTrue();
    }

    [Test]
    public async Task ExistsByNameAsync_Devuelve_FalseSiMismoNombrePerteneceAlMismoIdExcluido()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            Name = "Distribuciones Norte",
            Descripcion = "Zona norte"
        };
        await _context.Proveedores.InsertOneAsync(proveedor);

        // Act - Evaluamos si existe el nombre excluyendo el propio ID del proveedor (caso típico de update)
        var existe = await _repository.ExistsByNameAsync("Distribuciones Norte", id: proveedor.Id);

        // Assert
        existe.Should().BeFalse();
    }
}