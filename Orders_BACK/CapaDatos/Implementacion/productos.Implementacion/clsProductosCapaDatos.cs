using CapaDatos.Interfaz.productos.interfaz;
using CapaDto.Respuestas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CapaDatos.Implementacion.productos.Implementacion
{
    public class clsProductosCapaDatos : IProductosCapaDatos
    {
        private readonly IConfiguration _configuration;

        public clsProductosCapaDatos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection NuevaConexion() => new SqlConnection(_configuration.GetConnectionString("SQLConnectionString"));

        public async Task<List<ProductoDto>> ListarActivos()
        {
            const string query = "SELECT Sku, Nombre FROM ped.Productos WHERE Activo = 1 ORDER BY Sku";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);

            var productos = new List<ProductoDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                productos.Add(new ProductoDto { Sku = reader.GetString(0), Nombre = reader.GetString(1) });
            return productos;
        }

        public async Task<bool> ExisteActivo(string sku)
        {
            const string query = "SELECT COUNT(1) FROM ped.Productos WHERE Sku = @Sku AND Activo = 1";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 50).Value = sku;
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
    }
}
