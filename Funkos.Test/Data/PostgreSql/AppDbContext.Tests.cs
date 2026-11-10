using FluentAssertions;
using Funkos.DbContext;
using Funkos.idGenerators;
using Funkos.Infrastructure.Seed;
using Funkos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Funkos.Test.Data.PostgreSql;

[TestFixture]
public class AppDbContextTests
{
    private PostgreSqlContainer _postgresContainer = null!;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Se levanta el contenedor real de PostgreSQL una única vez para toda la suite
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("funkos_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Se destruye el contenedor al terminar todas las pruebas
        await _postgresContainer.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        var context = new AppDbContext(options);
        // Garantiza que las tablas de Identity y las entidades de tu app estén creadas
        context.Database.EnsureCreated();
        return context;
    }
    
    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_connectionString));

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }
    
    [Test]
    public void IdYoutube_Debe_Generar_Cadena_Valida_De_11_Caracteres()
    {
        // Act
        var id1 = IdYoutube.Next();
        var id2 = IdYoutube.Next();

        // Assert
        id1.Should().NotBeNullOrEmpty();
        id1.Length.Should().Be(11);
        IdYoutube.EsValido(id1).Should().BeTrue();

        id2.Should().NotBeNullOrEmpty();
        id2.Length.Should().Be(11);
        IdYoutube.EsValido(id2).Should().BeTrue();

        id1.Should().NotBe(id2);
    }

    [Test]
    public async Task SeedIdentityData_Debe_Crear_Roles_Y_Usuarios_Iniciales()
    {
        // Arrange
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Act
        await SeedIdentityData.SeedAsync(provider);

        // Assert
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        (await roleManager.RoleExistsAsync(UserRoles.Admin)).Should().BeTrue();
        (await roleManager.RoleExistsAsync(UserRoles.User)).Should().BeTrue();

        var adminUser = await userManager.FindByEmailAsync("admin@admin.com");
        adminUser.Should().NotBeNull();
        adminUser!.UserName.Should().Be("admin");
        (await userManager.IsInRoleAsync(adminUser, UserRoles.Admin)).Should().BeTrue();
        (await userManager.CheckPasswordAsync(adminUser, "Admin123!")).Should().BeTrue();

        var normalUser = await userManager.FindByEmailAsync("user@user.com");
        normalUser.Should().NotBeNull();
        normalUser!.UserName.Should().Be("user");
        (await userManager.IsInRoleAsync(normalUser, UserRoles.User)).Should().BeTrue();
        (await userManager.CheckPasswordAsync(normalUser, "User123!")).Should().BeTrue();
    }

    [Test]
    public async Task Debe_Crear_Categoria_Con_IdYoutube_Generado_Automaticamente()
    {
        // Arrange
        await using var context = CreateContext();
        var categoria = new Categoria
        {
            Name = "Anime Coleccionables",
            Descripcion = "Figuras exclusivas de series de anime populares."
        };

        // Act
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        // Assert
        categoria.IdY.Should().NotBeNullOrEmpty();
        categoria.IdY.Length.Should().Be(11); // Longitud exacta de YouTubeId
        IdYoutube.EsValido(categoria.IdY).Should().BeTrue();

        var fetched = await context.Categorias.FindAsync(categoria.IdY);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Anime Coleccionables");
    }

    [Test]
    public async Task Debe_Lanzar_Excepcion_Al_Duplicar_Nombre_De_Categoria()
    {
        // Arrange
        await using var context = CreateContext();
        
        var cat1 = new Categoria { Name = "Marvel Comics", Descripcion = "Funkos de Marvel" };
        var cat2 = new Categoria { Name = "Marvel Comics", Descripcion = "Repetida Marvel" };

        context.Categorias.Add(cat1);
        await context.SaveChangesAsync();

        context.Categorias.Add(cat2);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task Debe_Respetar_Check_Constraint_De_Precio_Minimo()
    {
        // Arrange
        await using var context = CreateContext();
        
        var categoria = new Categoria { Name = "DC Universe", Descripcion = "Superheroes DC" };
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        var funkoInvalido = new Funko
        {
            Name = "Batman Barato",
            Precio = 0.50m, // Viola el Check Constraint configurado (Precio >= 1.00)
            Stock = 10,
            CategoriaId = categoria.IdY,
            ProveedorId = "prov12345678901234567890"
        };

        context.Funkos.Add(funkoInvalido);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task Debe_Aplicar_Filtro_De_Query_Para_IsDeleted()
    {
        // Arrange
        await using var context = CreateContext();

        var categoria = new Categoria { Name = "Star Wars Series", Descripcion = "Galactic Funkos" };
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        var funkoActivo = new Funko
        {
            Name = "Darth Vader Standard",
            Precio = 15.00m,
            Stock = 5,
            IsDeleted = false,
            CategoriaId = categoria.IdY,
            ProveedorId = "prov12345678901234567890"
        };

        var funkoEliminadoLogico = new Funko
        {
            Name = "Stormtrooper Archivado",
            Precio = 12.00m,
            Stock = 0,
            IsDeleted = true, // Marcado como borrado lógico
            CategoriaId = categoria.IdY,
            ProveedorId = "prov12345678901234567890"
        };

        context.Funkos.AddRange(funkoActivo, funkoEliminadoLogico);
        await context.SaveChangesAsync();

        // Act
        // Usamos un nuevo contexto para comprobar que el QueryFilter actúa globalmente
        await using var queryContext = CreateContext();
        var listaFunkos = await queryContext.Funkos.ToListAsync();

        // Assert
        listaFunkos.Should().ContainSingle(f => f.Name == "Darth Vader Standard");
        listaFunkos.Should().NotContain(f => f.Name == "Stormtrooper Archivado");
    }

    [Test]
    public async Task Debe_Manejar_Concurrencia_Optimista_Correctamente()
    {
        // Arrange
        await using var contextSetup = CreateContext();
        var categoria = new Categoria { Name = "Concurrencia Cat", Descripcion = "Prueba de versiones" };
        contextSetup.Categorias.Add(categoria);
        await contextSetup.SaveChangesAsync();

        // Creamos dos instancias de contexto simulando concurrencia
        await using var context1 = CreateContext();
        await using var context2 = CreateContext();

        var catInstancia1 = await context1.Categorias.FindAsync(categoria.IdY);
        var catInstancia2 = await context2.Categorias.FindAsync(categoria.IdY);

        // Act - Usuario 1 modifica y guarda exitosamente
        catInstancia1!.Descripcion = "Modificado por Usuario 1";
        await context1.SaveChangesAsync();

        // Usuario 2 intenta modificar el registro cuya versión ya cambió en la BD
        catInstancia2!.Descripcion = "Modificado por Usuario 2";

        Func<Task> act = async () => await context2.SaveChangesAsync();

        // Assert - Debe saltar la excepción de conflicto por el token de concurrencia
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}