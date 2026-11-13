using CapaDatos.Implementacion.outbox.Implementacion;
using CapaDatos.Implementacion.pedidos.Implementacion;
using CapaDatos.Implementacion.productos.Implementacion;
using CapaDatos.Interfaz.outbox.interfaz;
using CapaDatos.Interfaz.pedidos.interfaz;
using CapaDatos.Interfaz.productos.interfaz;
using CapaNegocios.Implementacion.pedidos.Implementacion;
using CapaNegocios.Implementacion.productos.Implementacion;
using CapaNegocios.Interfaz.pedidos.interfaz;
using CapaNegocios.Interfaz.productos.interfaz;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Orders_Api.mensajeria;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Toda la configuración sensible llega por variables de entorno (docker-compose) o user-secrets (local).
// Si falta algo, el servicio no arranca y lo dice, en vez de fallar más tarde con un error confuso.
string Requerido(string clave) => builder.Configuration[clave] is { Length: > 0 } valor
    ? valor
    : throw new InvalidOperationException($"Falta configurar {clave}.");

string cadenaConexion = Requerido("ConnectionStrings:SQLConnectionString");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderFlow - Orders API",
        Version = "v1",
        Description = "Crea y consulta pedidos. Cada pedido nace en Pending y pasa a Confirmed o Rejected según la respuesta de Inventory."
    });
    opciones.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

// CORS solo si el front se sirve desde otro dominio (por ejemplo Azure Static Web Apps).
// En Docker no hace falta: nginx sirve el front y reenvía /api desde el mismo origen.
// Ejemplo: Cors__OrigenesPermitidos=https://<app>.azurestaticapps.net (varios separados por coma).
string[] origenesPermitidos = (builder.Configuration["Cors:OrigenesPermitidos"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (origenesPermitidos.Length > 0)
{
    builder.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica =>
        politica.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod()));
}

// /health/ready revisa SQL Server y RabbitMQ (MassTransit registra su propio check con la etiqueta "ready").
builder.Services.AddHealthChecks().AddSqlServer(cadenaConexion, name: "sqlserver", tags: new[] { "ready" });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservadoConsumer>();
    x.AddConsumer<StockRechazadoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            Requerido("RabbitMQ:Host"),
            ushort.TryParse(builder.Configuration["RabbitMQ:Puerto"], out ushort puerto) ? puerto : (ushort)5672,
            builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(Requerido("RabbitMQ:Usuario"));
                h.Password(Requerido("RabbitMQ:Password"));
            });

        // Errores transitorios (por ejemplo, SQL Server caído): se reintenta; si se agotan, el mensaje va a la cola *_error.
        cfg.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)));
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("orders", false));
    });
});

builder.Services.AddScoped<IPedidosCapaDatos, clsPedidosCapaDatos>();
builder.Services.AddScoped<IProductosCapaDatos, clsProductosCapaDatos>();
builder.Services.AddScoped<IOutboxCapaDatos, clsOutboxCapaDatos>();
builder.Services.AddScoped<IPedidosCapaNegocios, clsPedidosCapaNegocios>();
builder.Services.AddScoped<IProductosCapaNegocios, clsProductosCapaNegocios>();
builder.Services.AddHostedService<PublicadorOutboxService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "OrderFlow - Orders API");
}

if (origenesPermitidos.Length > 0)
    app.UseCors();

app.MapControllers();

// Liveness: el proceso responde (no revisa dependencias, para que Kubernetes no lo reinicie si cae SQL o RabbitMQ).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
// Readiness: puede atender tráfico porque SQL Server y RabbitMQ responden.
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registro => registro.Tags.Contains("ready") });

app.Run();

// Permite usar WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
