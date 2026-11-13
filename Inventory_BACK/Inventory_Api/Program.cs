using CapaDatos.Implementacion.reservas.Implementacion;
using CapaDatos.Implementacion.stock.Implementacion;
using CapaDatos.Interfaz.reservas.interfaz;
using CapaDatos.Interfaz.stock.interfaz;
using CapaNegocios.Implementacion.reservas.Implementacion;
using CapaNegocios.Implementacion.stock.Implementacion;
using CapaNegocios.Interfaz.reservas.interfaz;
using CapaNegocios.Interfaz.stock.interfaz;
using CapaNegocios.util;
using Inventory_Api.mensajeria;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Toda la configuración sensible llega por variables de entorno (docker-compose) o user-secrets (local).
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
        Title = "OrderFlow - Inventory",
        Version = "v1",
        Description = "Su trabajo principal es el consumidor de PedidoCreado. La API solo expone el stock para consulta."
    });
    opciones.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services.AddHealthChecks().AddSqlServer(cadenaConexion, name: "sqlserver", tags: new[] { "ready" });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PedidoCreadoConsumer>();

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

        // Errores transitorios (SQL Server caído, fallo al publicar la respuesta): se reintenta.
        // Un evento inválido no mejora reintentando: va directo a la cola inventory-pedido-creado_error.
        cfg.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            r.Ignore<EventoInvalidoException>();
        });
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("inventory", false));
    });
});

builder.Services.AddScoped<IReservasCapaDatos, clsReservasCapaDatos>();
builder.Services.AddScoped<IStockCapaDatos, clsStockCapaDatos>();
builder.Services.AddScoped<IReservasCapaNegocios, clsReservasCapaNegocios>();
builder.Services.AddScoped<IStockCapaNegocios, clsStockCapaNegocios>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "OrderFlow - Inventory");
}

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registro => registro.Tags.Contains("ready") });

app.Run();

public partial class Program;
