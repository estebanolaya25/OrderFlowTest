using CapaDatos.Interfaz.pedidos.interfaz;
using CapaDatos.util;
using CapaDto.Modelos;
using CapaDto.Respuestas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CapaDatos.Implementacion.pedidos.Implementacion
{
    public class clsPedidosCapaDatos : IPedidosCapaDatos
    {
        private const string ColumnasPedido = "PedidoId, ClienteNombre, Sku, Cantidad, Estado, MotivoRechazo, CreadoEn, ActualizadoEn";

        private readonly IConfiguration _configuration;

        public clsPedidosCapaDatos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection NuevaConexion() => new SqlConnection(_configuration.GetConnectionString("SQLConnectionString"));

        public async Task CrearPedido(NuevoPedido pedido, EventoOutbox evento)
        {
            const string query = @"
                INSERT INTO ped.Pedidos (PedidoId, ClienteNombre, Sku, Cantidad, Estado, CreadoEn, ActualizadoEn)
                VALUES (@PedidoId, @ClienteNombre, @Sku, @Cantidad, 'Pending', @CreadoEn, @CreadoEn)";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            // Si algo falla antes del Commit, el Dispose de la transacción hace rollback: no queda pedido sin evento.
            using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            using (var cmd = new SqlCommand(query, connection, transaction))
            {
                cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = pedido.PedidoId;
                cmd.Parameters.Add("@ClienteNombre", SqlDbType.NVarChar, 150).Value = pedido.ClienteNombre;
                cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 50).Value = pedido.Sku;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = pedido.Cantidad;
                cmd.Parameters.Add("@CreadoEn", SqlDbType.DateTime2).Value = pedido.CreadoEn;
                await cmd.ExecuteNonQueryAsync();
            }

            await clsOutboxSql.Agregar(connection, transaction, evento);
            await transaction.CommitAsync();
        }

        public async Task<PedidoDto?> ObtenerPedido(Guid pedidoId)
        {
            string query = $"SELECT {ColumnasPedido} FROM ped.Pedidos WHERE PedidoId = @PedidoId";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = pedidoId;

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? LeerPedido(reader) : null;
        }

        public async Task<List<PedidoDto>> ListarPedidos(int limite)
        {
            string query = $"SELECT TOP (@Limite) {ColumnasPedido} FROM ped.Pedidos ORDER BY CreadoEn DESC";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@Limite", SqlDbType.Int).Value = limite;

            var pedidos = new List<PedidoDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                pedidos.Add(LeerPedido(reader));
            return pedidos;
        }

        public async Task<ResultadoTransicion> AplicarTransicion(TransicionPedido transicion)
        {
            if (transicion.EstadosPermitidos.Length == 0)
                throw new ArgumentException("La transición debe indicar al menos un estado permitido.", nameof(transicion));

            const string queryMensaje = "INSERT INTO ped.MensajesProcesados (MensajeId, TipoMensaje) VALUES (@MensajeId, @TipoMensaje)";
            const string queryExiste = "SELECT COUNT(1) FROM ped.Pedidos WHERE PedidoId = @PedidoId";

            // El WHERE Estado IN (...) hace la transición atómica: si dos mensajes llegan a la vez, solo uno cambia la fila.
            string parametrosEstados = string.Join(", ", transicion.EstadosPermitidos.Select((_, i) => $"@Estado{i}"));
            string queryEstado = $@"
                UPDATE ped.Pedidos
                SET Estado = @NuevoEstado, MotivoRechazo = @Motivo, ActualizadoEn = SYSUTCDATETIME()
                WHERE PedidoId = @PedidoId AND Estado IN ({parametrosEstados})";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                using var cmd = new SqlCommand(queryMensaje, connection, transaction);
                cmd.Parameters.Add("@MensajeId", SqlDbType.UniqueIdentifier).Value = transicion.MensajeId;
                cmd.Parameters.Add("@TipoMensaje", SqlDbType.VarChar, 100).Value = transicion.TipoMensaje;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number is clsOutboxSql.ErrorClaveDuplicada or clsOutboxSql.ErrorIndiceUnico)
            {
                await transaction.RollbackAsync();
                return ResultadoTransicion.MensajeDuplicado;
            }

            int filas;
            using (var cmd = new SqlCommand(queryEstado, connection, transaction))
            {
                cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = transicion.PedidoId;
                cmd.Parameters.Add("@NuevoEstado", SqlDbType.VarChar, 20).Value = transicion.NuevoEstado;
                cmd.Parameters.Add("@Motivo", SqlDbType.NVarChar, 300).Value = (object?)transicion.Motivo ?? DBNull.Value;
                for (int i = 0; i < transicion.EstadosPermitidos.Length; i++)
                    cmd.Parameters.Add($"@Estado{i}", SqlDbType.VarChar, 20).Value = transicion.EstadosPermitidos[i];
                filas = await cmd.ExecuteNonQueryAsync();
            }

            ResultadoTransicion resultado = ResultadoTransicion.Aplicada;
            if (filas == 0)
            {
                using var cmd = new SqlCommand(queryExiste, connection, transaction);
                cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = transicion.PedidoId;
                bool existe = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                resultado = existe ? ResultadoTransicion.EstadoNoPermitido : ResultadoTransicion.NoEncontrado;
            }

            // Se confirma también cuando no hubo cambio: el mensaje queda registrado y no se vuelve a evaluar.
            await transaction.CommitAsync();
            return resultado;
        }

        // SQL Server guarda DATETIME2 sin zona; todas las fechas se graban en UTC, así que se marcan como tal
        // para que el JSON salga con "Z" y el navegador las convierta a la hora local.
        private static PedidoDto LeerPedido(SqlDataReader reader) => new()
        {
            Id = reader.GetGuid(0),
            ClienteNombre = reader.GetString(1),
            Sku = reader.GetString(2),
            Cantidad = reader.GetInt32(3),
            Estado = reader.GetString(4),
            MotivoRechazo = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreadoEn = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            ActualizadoEn = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)
        };
    }
}
