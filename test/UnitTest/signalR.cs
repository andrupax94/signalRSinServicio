using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
namespace Test.unitTest;
public class MyHubIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyHubIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Client_ShouldConnect_AndBeStored()
    {
        // Arrange
        var client = _factory.CreateDefaultClient();
        var serverUrl = client.BaseAddress.ToString().Replace("http://", "https://");

        var guid = Guid.NewGuid().ToString();

        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}chatHub?guid={guid}", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        // Act
        await hubConnection.StartAsync();

        // Verificar que el cliente está conectado
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        // Llamar al servidor para obtener el ConnectionId almacenado
        var storedConnectionId = await hubConnection.InvokeAsync<string>("GetConnectionIdByGuid", guid);

        // Verificar que el GUID fue almacenado correctamente con un ConnectionId asignado
        Assert.NotNull(storedConnectionId);
        Assert.Equal(hubConnection.ConnectionId, storedConnectionId);

        // Cleanup
        await hubConnection.StopAsync();
    }

}
/* INFO 
✅ Pros:
1️⃣ Verificación Completa: Asegura que el cliente no solo se conecta, sino que su GUID es almacenado correctamente con un ConnectionId.
2️⃣ Encapsulación Segura: No expone _connectedClients directamente, sino que usa un método controlado (GetConnectionIdByGuid).

❌ Contras:
1️⃣ Dependencia en el Hub: Si el método GetConnectionIdByGuid no existe en producción, el test no refleja el comportamiento real.
2️⃣ Posible Concurrencia: En entornos con múltiples clientes, la validación podría ser más compleja por conexiones simultáneas.

🎯 Conclusión:
Este enfoque valida correctamente que el cliente se conecta y su GUID se almacena, asegurando que el sistema maneja bien las conexiones. Sin embargo, introduce una pequeña dependencia en un método extra para pruebas, lo que podría requerir ajustes en producción. 🚀 */


