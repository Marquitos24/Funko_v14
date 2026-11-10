using System.Text;
using FluentAssertions;
using Funkos.Infrastructure.Storage;
using Funkos.Service.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Funkos.Test.Services;

[TestFixture]
public class FileSystemStorageServiceTest
{
   private Mock<IWebHostEnvironment> _mockEnv = null!;
    private Mock<ILogger<FileSystemStorageService>> _mockLogger = null!;
    private IOptions<StorageSettings> _options = null!;
    private string _tempRootPath = null!;
    private FileSystemStorageService _storageService = null!;

    [SetUp]
    public void SetUp()
    {
        // Creamos un directorio temporal único para cada test para aislar las pruebas de disco
        _tempRootPath = Path.Combine(Path.GetTempPath(), "FunkosTestStorage_" + Guid.NewGuid().ToString("N"));

        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(e => e.ContentRootPath).Returns(_tempRootPath);

        _mockLogger = new Mock<ILogger<FileSystemStorageService>>();

        var settings = new StorageSettings
        {
            RootPath = "uploads",
            MaxFileSize = 1024 * 1024, // 1 MB
            AllowedExtensions = new[] { ".jpg", ".png" },
            AllowedContentTypes = new[] { "image/jpeg", "image/png" },
            ImagesFolder = "images",
            DocumentsFolder = "documents"
        };

        _options = Options.Create(settings);

        _storageService = new FileSystemStorageService(
            _mockEnv.Object,
            _options,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        // Limpiamos la carpeta temporal después de cada test
        if (Directory.Exists(_tempRootPath))
        {
            try { Directory.Delete(_tempRootPath, true); } catch { }
        }
    }

    [Test]
    public async Task InitAsync_Crea_DirectoriosBaseEnDisco()
    {
        // Act
        await _storageService.InitAsync(CancellationToken.None);

        // Assert
        Directory.Exists(Path.Combine(_tempRootPath, "uploads")).Should().BeTrue();
        Directory.Exists(Path.Combine(_tempRootPath, "uploads", "images")).Should().BeTrue();
        Directory.Exists(Path.Combine(_tempRootPath, "uploads", "documents")).Should().BeTrue();
        Directory.Exists(Path.Combine(_tempRootPath, "uploads", "temp")).Should().BeTrue();
    }

    [Test]
    public async Task StoreAsync_IFormFile_Devuelve_FailureSiArchivoEsNuloOVacio()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        // Act
        var resultado = await _storageService.StoreAsync(mockFile.Object, null, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task StoreAsync_IFormFile_Devuelve_FailureSiExtensionNoPermitida()
    {
        // Arrange
        var content = "contenido de prueba";
        var fileName = "documento.txt";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(ms.Length);
        mockFile.Setup(f => f.ContentType).Returns("text/plain");
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

        // Act
        var resultado = await _storageService.StoreAsync(mockFile.Object, null, CancellationToken.None);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task StoreAsync_IFormFile_Devuelve_SuccessYGuardaArchivoCorrectamente()
    {
        // Arrange
        var content = "imagen falsa";
        var fileName = "test.jpg";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(ms.Length);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((stream, _) => ms.CopyToToAsyncSafe(stream))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _storageService.StoreAsync(mockFile.Object, "images", CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNullOrEmpty();
        
        // Verificamos que exista físicamente en el disco
        bool existe = _storageService.Exists(resultado.Value, "images");
        existe.Should().BeTrue();
    }

    [Test]
    public void GetUrl_Devuelve_RutaRelativaCorrectaConYSinCarpeta()
    {
        // Act & Assert
        _storageService.GetUrl("archivo.jpg", "images").Should().Be("/uploads/images/archivo.jpg");
        _storageService.GetUrl("archivo.jpg", null).Should().Be("/uploads/archivo.jpg");
    }

    [Test]
    public async Task DeleteAsync_Borra_ArchivoSiExisteYDevuelveTrue()
    {
        // Arrange
        var fileName = "borrar.jpg";
        var folderPath = Path.Combine(_tempRootPath, "uploads", "images");
        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllTextAsync(filePath, "contenido");

        // Act
        var eliminado = await _storageService.DeleteAsync(fileName, "images", CancellationToken.None);

        // Assert
        eliminado.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_Devuelve_FalseSiArchivoNoExiste()
    {
        // Act
        var eliminado = await _storageService.DeleteAsync("no_existe.jpg", "images", CancellationToken.None);

        // Assert
        eliminado.Should().BeFalse();
    }
}

internal static class StreamExtensions
{
    public static void CopyToToAsyncSafe(this Stream source, Stream destination)
    {
        source.CopyTo(destination);
    }
}