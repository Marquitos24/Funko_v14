using System.Net;
using FluentAssertions;
using Funkos.Middleware;
using Microsoft.AspNetCore.Http;

namespace Funkos.Test.Middleware;

[TestFixture]
public class RateLimitMiddlewareTest
{
    [Test]
    public async Task InvokeAsync_ExtraeCorrectamenteIpDeXForwardedFor()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/funkos";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18";
        context.Response.Body = new MemoryStream();

        using var state = new RateLimitingState();
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, state);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers["RateLimit-Limit"].Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task InvokeAsync_ExtraeCorrectamenteIpDeXRealIpSiNoHayForwarded()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/funkos";
        context.Request.Headers["X-Real-IP"] = "192.168.1.100";
        context.Response.Body = new MemoryStream();

        using var state = new RateLimitingState();
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, state);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task InvokeAsync_NormalizaRutasConMayusculasYBarraFinal()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/API/Funkos/"; // Mayúsculas y barra final
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Response.Body = new MemoryStream();

        using var state = new RateLimitingState();
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, state);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers["RateLimit-Limit"].Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task InvokeAsync_AplicaVentanaDeEscrituraEnPeticionesPost()
    {
        // Arrange
        using var state = new RateLimitingState();
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, state);

        HttpContext? ultimoContexto = null;

        // El límite de escritura (POST) es de 20 por minuto. Agotamos la ventana.
        for (int i = 0; i < 25; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/funkos";
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);
            ultimoContexto = context;
        }

        // Assert
        ultimoContexto.Should().NotBeNull();
        ultimoContexto!.Response.StatusCode.Should().Be(429);
        ultimoContexto.Response.Headers["Retry-After"].Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task InvokeAsync_AplicaVentanaEstrictaEnRutasDeAutenticacion()
    {
        // Arrange
        using var state = new RateLimitingState();
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, state);

        HttpContext? ultimoContexto = null;

        // El límite de auth es de 10 por minuto. Agotamos la ventana.
        for (int i = 0; i < 12; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/auth/login";
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.99");
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);
            ultimoContexto = context;
        }

        // Assert
        ultimoContexto.Should().NotBeNull();
        ultimoContexto!.Response.StatusCode.Should().Be(429);
    }

    [Test]
    public void RateLimitingState_Dispose_LiberaParticionesCorrectamente()
    {
        // Arrange
        var state = new RateLimitingState();
        
        // Act & Assert
        Assert.DoesNotThrow(() => state.Dispose());
    }
}