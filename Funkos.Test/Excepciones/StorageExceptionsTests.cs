using FluentAssertions;
using Funkos.Exceptions;

namespace Funkos.Test.Excepciones;

[TestFixture]
public class StorageExceptionsTests
{
    [Test]
    public void FileSizeExceededException_InicializaMensajeYPropiedadesCorrectamente()
    {
        // Arrange
        var message = "El archivo excede el tamaño máximo";
        long maxSize = 5000000;
        long actualSize = 7500000;

        // Act
        var exception = new FileSizeExceededException(message, maxSize, actualSize);

        // Assert
        exception.Message.Should().Be(message);
        exception.MaxSize.Should().Be(maxSize);
        exception.ActualSize.Should().Be(actualSize);
    }

    [Test]
    public void FileSizeExceededException_ConstructorSimpleInicializaMensaje()
    {
        // Arrange
        var message = "Error de tamaño";

        // Act
        var exception = new FileSizeExceededException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.MaxSize.Should().Be(0);
        exception.ActualSize.Should().Be(0);
    }

    [Test]
    public void InvalidFileTypeException_InicializaMensajeCorrectamente()
    {
        // Arrange
        var message = "Tipo de archivo no permitido";

        // Act
        var exception = new InvalidFileTypeException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.FileType.Should().BeNull();
        exception.AllowedTypes.Should().BeNull();
    }
}