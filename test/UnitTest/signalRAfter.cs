using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Test.unitTest;

public class SignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Mantener la configuración original de Program.cs
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("https_port", "5001");
            builder.UseSetting("http_port", "5000");
        });
    }


    [Theory]
    [InlineData("https://test1.example.com", true)]  // Permitido
    [InlineData("https://malicious-site.com", false)] // Bloqueado
    [InlineData("https://malicions-sitejhh.com", false)] // Bloqueado
    public async Task Cors_Should_Block_Disallowed_Origins(string origin, bool shouldAllow)
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/chatHub");
        request.Headers.Add("Origin", origin);

        // Act
        var response = await client.SendAsync(request);

        bool hasCorsHeaders = response.Headers.Contains("Access-Control-Allow-Origin");

        // Assert
        Assert.Equal(shouldAllow, hasCorsHeaders);
    }

    /* INFO  ¿Qué está probando realmente esta prueba?
    ✅  Verifica que el servidor responde correctamente a peticiones con distintos Origin.
    ✅  Confirma si el servidor devuelve la cabecera Access-Control-Allow-Origin para los dominios permitidos.
    ❌  No simula completamente un navegador(porque un navegador sí bloquearía la solicitud).
    📌 Para una prueba más completa, se puede complementar con una prueba en un navegador real. */
}
