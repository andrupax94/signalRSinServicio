using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(

                        "https://test1.example.com",
                        "https://localhost:7296",
                        "https://blazorclient-gdd2ese0aebrd7b3.canadacentral-01.azurewebsites.net",
                        "https://andrespa.servecounterstrike.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
            });
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
        });
        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
            app.UseCors("AllowAll");
            //app.UseCors();
            app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapHub<MyHub>("/chatHub");
        app.MapGet("/", () => Results.Content("<html><body><h1>Bienvenido</h1></body></html>", "text/html"));

        app.Run();
    }
}

public class MyHub : Hub
{
    private readonly ILogger<MyHub> _logger;
    public static Dictionary<string, string> _connectedClients = new();

    public MyHub(ILogger<MyHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente conectado: {ConnectionId}", Context.ConnectionId);

        var httpContext = Context.GetHttpContext();
        var guid = httpContext.Request.Query["guid"].ToString();
        if (string.IsNullOrEmpty(guid))
        {
            guid = Context.ConnectionId;
        }

        // Asocia el GUID con el ConnectionId y almacénalo en _connectedClients
        _connectedClients[guid] = Context.ConnectionId;

        return base.OnConnectedAsync();
    }

    public string? GetConnectionIdByGuid(string guid)
    {
#if DEBUG // Solo se compila en modo Debug o pruebas
        return _connectedClients.TryGetValue(guid, out var connectionId) ? connectionId : null;
#else
        throw new InvalidOperationException("Esta función no está disponible en producción.");
#endif
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);

        // Encuentra el GUID asociado con el ConnectionId y elimínalo del diccionario
        var guid = _connectedClients.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (guid != null)
        {
            _connectedClients.Remove(guid);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessageToClient(string guid, string message)
    {
        if (_connectedClients.TryGetValue(guid, out var targetConnectionId))
        {
            _logger.LogInformation("Enviando mensaje de {Sender} a {Receiver}: {Message}",
                Context.ConnectionId, targetConnectionId, message);

            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", message);
        }
        else
        {
            _logger.LogWarning("Intento de envío fallido: {Target} no está conectado", guid);
        }
    }
}
