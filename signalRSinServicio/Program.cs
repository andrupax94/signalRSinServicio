using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;

public class Program
{
    public static void Main(string[] args)
    {
        var options = new WebApplicationOptions
        {
            ContentRootPath = Directory.GetCurrentDirectory()
        };

        var builder = WebApplication.CreateBuilder(options);

        // Configuración del servicio
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

        // Configuración de autenticación JWT
        var key = Encoding.ASCII.GetBytes("tu-clave-secreta-super-segura");
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        var app = builder.Build();

        // Configuración del pipeline de solicitud HTTP
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Agregar middleware de autenticación y autorización
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapHub<MyHub>("/chatHub");

        app.Run();
    }
}


public class MyHub : Hub
    {
        private readonly ILogger<MyHub> _logger;
        public static Dictionary<string, string> _connectedClients = new();
        public static Dictionary<string, string> _clientsWithGuid = new();
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
            else
            {
                _clientsWithGuid[Context.ConnectionId] = guid;
            }

            // Asocia el GUID con el ConnectionId y almacénalo en _connectedClients
            _connectedClients[guid] = Context.ConnectionId;

            return base.OnConnectedAsync();
        }

        public string? GetConnectionIdByGuid(string guid)
        {

            return _connectedClients.TryGetValue(guid, out var connectionId) ? connectionId : null;

        }
        public List<string> GetOtherConnectedClientsGuids()
        {
            var currentConnectionId = Context.ConnectionId;
            var currentGuid = _clientsWithGuid.ContainsKey(currentConnectionId) ? _clientsWithGuid[currentConnectionId] : null;

            return _clientsWithGuid
                .Where(x => x.Value != currentGuid)
                .Select(x => x.Value)
                .ToList();

        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);

            // Encuentra el GUID asociado con el ConnectionId y elimínalo del diccionario
            var conectionId = _connectedClients.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            var guid = _clientsWithGuid.FirstOrDefault(x => x.Key == Context.ConnectionId).Key;
            if (conectionId != null)
            {
                _connectedClients.Remove(conectionId);
                if (guid != null)
                {
                    _clientsWithGuid.Remove(guid);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToClient(string guid, string message)
        {
            if (_connectedClients.TryGetValue(guid, out var targetConnectionId))
            {
                _logger.LogInformation("Enviando mensaje de {Sender} a {Receiver}: {Message}",
                    Context.ConnectionId, targetConnectionId, message);

                await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", Context.ConnectionId, message);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", Context.ConnectionId, misFunciones.Mensajes.GenerateMessaje("El Cliente Destino No esta Conectado"));
                _logger.LogWarning("Intento de envío fallido: {Target} no está conectado", guid);
            }
        }
    }

