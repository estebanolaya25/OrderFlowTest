using CapaDatos.Interfaz.stock.interfaz;
using CapaDto.Respuestas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CapaDatos.Implementacion.stock.Implementacion
{
    public class clsStockCapaDatos : IStockCapaDatos
    {
        private readonly IConfiguration _configuration;

        public clsStockCapaDatos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection NuevaConexion() => new SqlConnection(_configuration.GetConnectionString("SQLConnectionString"));

        public async Task<List<StockDto>> Listar()
        {
            const string query = "SELECT Sku, Nombre, Disponible, ActualizadoEn FROM inv.Stock ORDER BY Sku";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);

            var stock = new List<StockDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stock.Add(new StockDto
                {
                    Sku = reader.GetString(0),
                    Nombre = reader.GetString(1),
                    Disponible = reader.GetInt32(2),
                    ActualizadoEn = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc)
                });
            }
            return stock;
        }
    }
}
