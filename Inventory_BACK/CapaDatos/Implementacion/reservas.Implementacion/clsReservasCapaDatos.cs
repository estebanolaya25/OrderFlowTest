using CapaDatos.Interfaz.reservas.interfaz;
using CapaDatos.util;
using CapaDto.Modelos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CapaDatos.Implementacion.reservas.Implementacion
{
    public class clsReservasCapaDatos : IReservasCapaDatos
    {
        private const string TipoMensaje = "PedidoCreado";

        private readonly IConfiguration _configuration;

        public clsReservasCapaDatos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection NuevaConexion() => new SqlConnection(_configuration.GetConnectionString("SQLConnectionString"));

        public async Task<ResultadoProcesamiento> Procesar(SolicitudReserva solicitud)
        {
            const string queryMensaje = "INSERT INTO inv.MensajesProcesados (MensajeId, TipoMensaje) VALUES (@MensajeId, @TipoMensaje)";

            // Descuento atómico: la condición Disponible >= @Cantidad se evalúa y aplica en la misma sentencia,
            // así dos pedidos concurrentes nunca dejan el stock en negativo (además del CHECK de la tabla).
            const string queryDescontar = @"
                UPDATE inv.Stock
                SET Disponible = Disponible - @Cantidad, ActualizadoEn = SYSUTCDATETIME()
                WHERE Sku = @Sku AND @Cantidad > 0 AND Disponible >= @Cantidad";

            const string queryDisponible = "SELECT Disponible FROM inv.Stock WHERE Sku = @Sku";

            const string queryReserva = @"
                INSERT INTO inv.Reservas (PedidoId, MensajeId, EventoRespuestaId, Sku, Cantidad, Resultado, Motivo, CreadoEn)
                VALUES (@PedidoId, @MensajeId, @EventoRespuestaId, @Sku, @Cantidad, @Resultado, @Motivo, @CreadoEn)";

            using var connection = NuevaConexion();
            await connection.OpenAsync();
            using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            // 1. Idempotencia por evento: la PK de MensajesProcesados rechaza el mismo EventoId por segunda vez.
            try
            {
                using var cmd = new SqlCommand(queryMensaje, connection, transaction);
                cmd.Parameters.Add("@MensajeId", SqlDbType.UniqueIdentifier).Value = solicitud.MensajeId;
                cmd.Parameters.Add("@TipoMensaje", SqlDbType.VarChar, 100).Value = TipoMensaje;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number is clsSqlErrores.ErrorClaveDuplicada or clsSqlErrores.ErrorIndiceUnico)
            {
                await transaction.RollbackAsync();
                Reserva original = await ObtenerReserva(connection, null, solicitud.PedidoId)
                    ?? throw new InvalidOperationException($"El mensaje {solicitud.MensajeId} figura como procesado pero no tiene reserva.");
                return new ResultadoProcesamiento { Duplicado = true, Reserva = original };
            }

            // 2. Idempotencia por pedido: un evento distinto para un pedido que ya tiene resultado tampoco descuenta.
            Reserva? existente = await ObtenerReserva(connection, transaction, solicitud.PedidoId);
            if (existente != null)
            {
                await transaction.CommitAsync();
                return new ResultadoProcesamiento { Duplicado = true, Reserva = existente };
            }

            // 3. Intentar el descuento.
            int filas;
            using (var cmd = new SqlCommand(queryDescontar, connection, transaction))
            {
                cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 50).Value = solicitud.Sku;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = solicitud.Cantidad;
                filas = await cmd.ExecuteNonQueryAsync();
            }

            string resultado = ResultadoReserva.Reservado;
            string? motivo = null;
            if (filas == 0)
            {
                using var cmd = new SqlCommand(queryDisponible, connection, transaction);
                cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 50).Value = solicitud.Sku;
                object? disponible = await cmd.ExecuteScalarAsync();

                resultado = ResultadoReserva.Rechazado;
                motivo = disponible is null or DBNull
                    ? MotivosRechazo.SkuInexistente(solicitud.Sku)
                    : MotivosRechazo.StockInsuficiente(solicitud.Cantidad, Convert.ToInt32(disponible));
            }

            // 4. Guardar el resultado. Si otro hilo registró el mismo pedido a la vez, la PK falla, la transacción
            //    se revierte (el descuento también) y el reintento de MassTransit cae en el paso 2.
            var reserva = new Reserva
            {
                PedidoId = solicitud.PedidoId,
                EventoRespuestaId = solicitud.EventoRespuestaId,
                Sku = solicitud.Sku,
                Cantidad = solicitud.Cantidad,
                Resultado = resultado,
                Motivo = motivo,
                CreadoEn = DateTime.UtcNow
            };

            using (var cmd = new SqlCommand(queryReserva, connection, transaction))
            {
                cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = reserva.PedidoId;
                cmd.Parameters.Add("@MensajeId", SqlDbType.UniqueIdentifier).Value = solicitud.MensajeId;
                cmd.Parameters.Add("@EventoRespuestaId", SqlDbType.UniqueIdentifier).Value = reserva.EventoRespuestaId;
                cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 50).Value = reserva.Sku;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = reserva.Cantidad;
                cmd.Parameters.Add("@Resultado", SqlDbType.VarChar, 20).Value = reserva.Resultado;
                cmd.Parameters.Add("@Motivo", SqlDbType.NVarChar, 300).Value = (object?)reserva.Motivo ?? DBNull.Value;
                cmd.Parameters.Add("@CreadoEn", SqlDbType.DateTime2).Value = reserva.CreadoEn;
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new ResultadoProcesamiento { Duplicado = false, Reserva = reserva };
        }

        private static async Task<Reserva?> ObtenerReserva(SqlConnection connection, SqlTransaction? transaction, Guid pedidoId)
        {
            const string query = @"
                SELECT PedidoId, EventoRespuestaId, Sku, Cantidad, Resultado, Motivo, CreadoEn
                FROM inv.Reservas WHERE PedidoId = @PedidoId";

            using var cmd = new SqlCommand(query, connection, transaction);
            cmd.Parameters.Add("@PedidoId", SqlDbType.UniqueIdentifier).Value = pedidoId;
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new Reserva
            {
                PedidoId = reader.GetGuid(0),
                EventoRespuestaId = reader.GetGuid(1),
                Sku = reader.GetString(2),
                Cantidad = reader.GetInt32(3),
                Resultado = reader.GetString(4),
                Motivo = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreadoEn = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)
            };
        }
    }
}
