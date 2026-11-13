using CapaDatos.Implementacion.reservas.Implementacion;
using CapaDto.Modelos;
using CapaNegocios.Implementacion.reservas.Implementacion;
using CapaNegocios.util;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Contratos.Pedidos;
using System.Text.RegularExpressions;
using Testcontainers.MsSql;

namespace Inventory_Tests
{
    // Prueba de integración contra un SQL Server real (Testcontainers levanta un contenedor desechable).
    // La idempotencia vive en la base de datos (PK de MensajesProcesados y Reservas + UPDATE condicional),
    // así que un mock no la demostraría: hay que ejecutar el SQL de verdad. Requiere Docker en ejecución.
    public class IdempotenciaReservaTests : IAsyncLifetime
    {
        private const string Sku = "ABC-01";
        private const int StockInicial = 100;

        private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        private string _cadenaConexion = string.Empty;

        public async Task InitializeAsync()
        {
            await _sqlServer.StartAsync();
            _cadenaConexion = _sqlServer.GetConnectionString();

            // Mismo script que aplica db-init en docker-compose, separado por los GO.
            string script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "BaseDatos", "01_esquema.sql"));
            foreach (string lote in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(lote))
                    await Ejecutar(lote);
            }

            await Ejecutar($"INSERT INTO inv.Stock (Sku, Nombre, Disponible) VALUES (N'{Sku}', N'Teclado', {StockInicial})");
        }

        public Task DisposeAsync() => _sqlServer.DisposeAsync().AsTask();

        [Fact]
        public async Task ElMismoPedidoCreadoProcesadoDosVeces_DescuentaElStockUnaSolaVez()
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SQLConnectionString"] = _cadenaConexion })
                .Build();
            var negocios = new clsReservasCapaNegocios(new clsReservasCapaDatos(configuracion), NullLogger<clsReservasCapaNegocios>.Instance);

            var evento = new PedidoCreado { EventoId = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Sku = Sku, Cantidad = 2, OcurridoEn = DateTime.UtcNow };

            RespuestaReserva primera = await negocios.ProcesarPedidoCreado(evento);
            RespuestaReserva segunda = await negocios.ProcesarPedidoCreado(evento);   // reentrega de RabbitMQ

            // El stock solo bajó una vez.
            Assert.Equal(StockInicial - 2, await Escalar<int>($"SELECT Disponible FROM inv.Stock WHERE Sku = N'{Sku}'"));
            Assert.Equal(1, await Escalar<int>($"SELECT COUNT(*) FROM inv.Reservas WHERE PedidoId = '{evento.PedidoId}'"));

            // La primera reservó; la segunda se reconoció como duplicada y repite exactamente la misma respuesta
            // (mismo EventoId), para que Orders también la descarte.
            Assert.False(primera.Duplicado);
            Assert.IsType<StockReservado>(primera.Evento);
            Assert.True(segunda.Duplicado);
            Assert.IsType<StockReservado>(segunda.Evento);
            Assert.Equal(primera.EventoId, segunda.EventoId);
        }

        private async Task Ejecutar(string sql)
        {
            await using var connection = new SqlConnection(_cadenaConexion);
            await connection.OpenAsync();
            await using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<T> Escalar<T>(string sql)
        {
            await using var connection = new SqlConnection(_cadenaConexion);
            await connection.OpenAsync();
            await using var cmd = new SqlCommand(sql, connection);
            return (T)Convert.ChangeType((await cmd.ExecuteScalarAsync())!, typeof(T));
        }
    }
}
