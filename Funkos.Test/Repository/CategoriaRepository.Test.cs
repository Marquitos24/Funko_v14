using FluentAssertions;
using Funkos.DbContext;
using Funkos.Models;
using Funkos.Repositories.Categoria;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Funkos.Test.Repository;

[TestFixture]
public class CategoriaRepositoryTest
{
    private PostgreSqlContainer _postgresContainer = null!;
    private string _connectionString = null!;
    private AppDbContext _context = null!;
    private CategoriaRepository _repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("funkos_categoria_repo_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgresContainer.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        _repository = new CategoriaRepository(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task FindAllAsync_Devuelve_ListaDeCategorias()
    {
        // Arrange
        _context.Categorias.Add(new Categoria { Name = "Anime", Descripcion = "Figuras de anime" });
        _context.Categorias.Add(new Categoria { Name = "Marvel", Descripcion = "Superhéroes Marvel" });
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _repository.FindAllAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);
        resultado.Should().Contain(c => c.Name == "Anime");
        resultado.Should().Contain(c => c.Name == "Marvel");
    }

    [Test]
    public async Task FindByIdAsync_Devuelve_CategoriaConFunkosSiExiste()
    {
        // Arrange
        var categoria = new Categoria { Name = "Star Wars", Descripcion = "Galactic collections" };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _repository.FindByIdAsync(categoria.IdY);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Name.Should().Be("Star Wars");
        resultado.Funkos.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_Devuelve_NuloSiNoExiste()
    {
        // Act
        var resultado = await _repository.FindByIdAsync("ABC12345678");

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_Devuelve_CategoriaCreadaConIdYoutubeGenerado()
    {
        // Arrange
        var nuevaCategoria = new Categoria
        {
            Name = "DC Comics",
            Descripcion = "Superhéroes DC"
        };

        // Act
        var resultado = await _repository.CreateAsync(nuevaCategoria);

        // Assert
        resultado.Should().NotBeNull();
        resultado.IdY.Should().NotBeNullOrEmpty();
        resultado.IdY.Length.Should().Be(11);
        resultado.Name.Should().Be("DC Comics");
        resultado.CreateAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_Devuelve_CategoriaModificadaYConVersionIncrementada()
    {
        // Arrange
        var categoria = new Categoria { Name = "Disney Old", Descripcion = "Clásicos" };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        var datosActualizados = new Categoria
        {
            Name = "Disney Updated",
            Descripcion = "Clásicos renovados"
        };

        // Act
        var resultado = await _repository.UpdateAsync(categoria.IdY, datosActualizados);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Name.Should().Be("Disney Updated");
        resultado.Descripcion.Should().Be("Clásicos renovados");
        resultado.Version.Should().Be(2); // Valida incremento de versión para concurrencia
        resultado.UpdateAt.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_Devuelve_NuloSiCategoriaNoExiste()
    {
        // Arrange
        var datosActualizados = new Categoria { Name = "Inexistente", Descripcion = "Test" };

        // Act
        var resultado = await _repository.UpdateAsync("INVALIDID11", datosActualizados);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_TrueYEliminaCategoria()
    {
        // Arrange
        var categoria = new Categoria { Name = "Temporal", Descripcion = "Para borrar" };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        // Act
        var exito = await _repository.DeleteAsync(categoria.IdY);

        // Assert
        exito.Should().BeTrue();

        var busqueda = await _repository.FindByIdAsync(categoria.IdY);
        busqueda.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_FalseSiCategoriaNoExiste()
    {
        // Act
        var exito = await _repository.DeleteAsync("NOTEXIST11");

        // Assert
        exito.Should().BeFalse();
    }

    [Test]
    public async Task ExistsAsync_Devuelve_TrueSiIdExiste()
    {
        // Arrange
        var categoria = new Categoria { Name = "Gamer", Descripcion = "Videojuegos" };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        // Act
        var existe = await _repository.ExistsAsync(categoria.IdY);

        // Assert
        existe.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_Devuelve_FalseSiIdNoExiste()
    {
        // Act
        var existe = await _repository.ExistsAsync("FAKEID11111");

        // Assert
        existe.Should().BeFalse();
    }

    [Test]
    public async Task ExistsByNameAsync_Devuelve_TrueSiNombreYaExiste()
    {
        // Arrange
        _context.Categorias.Add(new Categoria { Name = "Terror", Descripcion = "Películas de terror" });
        await _context.SaveChangesAsync();

        // Act
        var existe = await _repository.ExistsByNameAsync("Terror");

        // Assert
        existe.Should().BeTrue();
    }

    [Test]
    public async Task ExistsByNameAsync_Devuelve_FalseSiNombreNoExiste()
    {
        // Act
        var existe = await _repository.ExistsByNameAsync("Comedia");

        // Assert
        existe.Should().BeFalse();
    }
}