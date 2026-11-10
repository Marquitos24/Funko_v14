using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Funkos.Dto.Proveedor;
using Funkos.Models;
using Funkos.RealTime.Notificaciones;
using Funkos.Repositories;
using Funkos.Repositories.Proveedor;
using Funkos.Service.Proveedor;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;

namespace Funkos.Test.Services;

[TestFixture]
public class ProveedorServiceTest
{
    private Mock<IProveedorRepository> _mockRepository = null!;
    private Mock<IFunkoRepository> _mockFunkoRepository = null!;
    private Mock<IValidator<ProveedorCreateDto>> _mockValidatorCreate = null!;
    private Mock<IValidator<ProveedorUpdateDto>> _mockValidatorUpdate = null!;
    private Mock<ILogger<ProveedorService>> _mockLogger = null!;
    private Mock<IHubContext<NotificacionesHub>> _mockHubContext = null!;
    private Mock<IHubClients> _mockHubClients = null!;
    private Mock<IClientProxy> _mockClientProxy = null!;

    private ProveedorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IProveedorRepository>();
        _mockFunkoRepository = new Mock<IFunkoRepository>();
        _mockValidatorCreate = new Mock<IValidator<ProveedorCreateDto>>();
        _mockValidatorUpdate = new Mock<IValidator<ProveedorUpdateDto>>();
        _mockLogger = new Mock<ILogger<ProveedorService>>();
        
        // Setup para SignalR mocks
        _mockHubContext = new Mock<IHubContext<NotificacionesHub>>();
        _mockHubClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockHubClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockHubContext.Setup(hub => hub.Clients).Returns(_mockHubClients.Object);
        
        _mockClientProxy
            .Setup(client => client.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new ProveedorService(
            _mockRepository.Object,
            _mockFunkoRepository.Object,
            _mockValidatorCreate.Object,
            _mockValidatorUpdate.Object,
            _mockLogger.Object,
            _mockHubContext.Object
        );
    }

    [Test]
    public async Task GetAllAsync_Devuelve_SuccessConListaDeProveedores()
    {
        // Arrange
        var proveedores = new List<Proveedor>
        {
            new Proveedor { Id = ObjectId.GenerateNewId(), Name = "Distribuidor A", Descripcion = "Desc A", CreateAt = DateTime.UtcNow }
        };

        _mockRepository
            .Setup(r => r.FindAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedores);

        // Act
        var resultado = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_SuccessSiProveedorExiste()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var proveedor = new Proveedor
        {
            Id = id,
            Name = "Distribuidor B",
            Descripcion = "Desc B",
            CreateAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedor);

        // Act
        var resultado = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Distribuidor B");
    }

    [Test]
    public async Task GetByIdAsync_Devuelve_FailureSiIdEsVacio()
    {
        // Act
        var resultado = await _service.GetByIdAsync(ObjectId.Empty, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddAsync_Devuelve_FailureSiValidacionFalla()
    {
        // Arrange
        var createDto = new ProveedorCreateDto { Name = "", Descripcion = "" };

        _mockValidatorCreate
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "El nombre es obligatorio") }));

        // Act
        var resultado = await _service.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddAsync_Devuelve_SuccessAlCrearCorrectamente()
    {
        // Arrange
        var createDto = new ProveedorCreateDto { Name = "Nuevo Proveedor", Descripcion = "Descripción" };
        var id = ObjectId.GenerateNewId();

        _mockValidatorCreate
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockRepository
            .Setup(r => r.ExistsByNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proveedor p, CancellationToken _) =>
            {
                p.Id = id;
                return p;
            });

        // Act
        var resultado = await _service.AddAsync(createDto, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Nuevo Proveedor");
        
        // Verificamos que se notificó por SignalR
        _mockClientProxy.Verify(
            client => client.SendCoreAsync("ProveedorCreado", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task DeleteAsync_Devuelve_ErrorSiTieneFunkosAsociados()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var proveedor = new Proveedor { Id = id, Name = "Proveedor con Funkos", Descripcion = "descripcion"};

        _mockRepository
            .Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedor);

        _mockFunkoRepository
            .Setup(f => f.ExistsFunkosPAsync(id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // 3 funkos asociados

        // Act
        var resultado = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_SuccessAlEliminarCorrectamente()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var proveedor = new Proveedor { Id = id, Name = "Proveedor Libre", Descripcion = "descripcion"};

        _mockRepository
            .Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedor);

        _mockFunkoRepository
            .Setup(f => f.ExistsFunkosPAsync(id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        
        // Verificamos que se notificó por SignalR la eliminación
        _mockClientProxy.Verify(
            client => client.SendCoreAsync("ProveedorEliminado", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}