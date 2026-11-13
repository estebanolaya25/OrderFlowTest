using CapaDatos.Interfaz.reservas.interfaz;
using CapaDto.Modelos;
using CapaNegocios.Interfaz.reservas.interfaz;
using CapaNegocios.util;
using Microsoft.Extensions.Logging;
using OrderFlow.Contratos.Pedidos;

namespace CapaNegocios.Implementacion.reservas.Implementacion
{
    public class clsReservasCapaNegocios : IReservasCapaNegocios
    {
        private readonly IReservasCapaDatos _reservasCapaDatos;
        private readonly ILogger<clsReservasCapaNegocios> _logger;

        public clsReservasCapaNegocios(IReservasCapaDatos reservasCapaDatos, ILogger<clsReservasCapaNegocios> logger)
        {
            _reservasCapaDatos = reservasCapaDatos;
            _logger = logger;
        }

        public async Task<RespuestaReserva> ProcesarPedidoCreado(PedidoCreado evento)
        {
            // Orders ya valida, pero Inventory no confía en el mensaje: una cantidad negativa sumaría stock.
            if (evento.EventoId == Guid.Empty || evento.PedidoId == Guid.Empty)
                throw new EventoInvalidoException("PedidoCreado sin EventoId o PedidoId.");
            if (string.IsNullOrWhiteSpace(evento.Sku))
                throw new EventoInvalidoException($"PedidoCreado {evento.EventoId} sin SKU.");
            if (evento.Cantidad <= 0)
                throw new EventoInvalidoException($"PedidoCreado {evento.EventoId} con cantidad inválida ({evento.Cantidad}).");

            ResultadoProcesamiento resultado = await _reservasCapaDatos.Procesar(new SolicitudReserva
            {
                MensajeId = evento.EventoId,
                PedidoId = evento.PedidoId,
                Sku = evento.Sku.Trim(),
                Cantidad = evento.Cantidad,
                EventoRespuestaId = Guid.NewGuid()
            });

            Reserva reserva = resultado.Reserva;
            if (resultado.Duplicado)
                _logger.LogInformation("Pedido {PedidoId} ya procesado ({Resultado}); stock sin cambios, se republica el resultado.", reserva.PedidoId, reserva.Resultado);
            else if (reserva.Resultado == ResultadoReserva.Reservado)
                _logger.LogInformation("Pedido {PedidoId}: reservadas {Cantidad} unidades de {Sku}.", reserva.PedidoId, reserva.Cantidad, reserva.Sku);
            else
                _logger.LogInformation("Pedido {PedidoId} rechazado: {Motivo}", reserva.PedidoId, reserva.Motivo);

            return new RespuestaReserva
            {
                Evento = CrearEventoRespuesta(reserva),
                EventoId = reserva.EventoRespuestaId,
                Duplicado = resultado.Duplicado
            };
        }

        // La respuesta se arma siempre desde la reserva guardada, no desde el evento recibido:
        // así una republicación es idéntica a la publicación original.
        private static object CrearEventoRespuesta(Reserva reserva) => reserva.Resultado == ResultadoReserva.Reservado
            ? new StockReservado
            {
                EventoId = reserva.EventoRespuestaId,
                PedidoId = reserva.PedidoId,
                Sku = reserva.Sku,
                Cantidad = reserva.Cantidad,
                OcurridoEn = reserva.CreadoEn
            }
            : new StockRechazado
            {
                EventoId = reserva.EventoRespuestaId,
                PedidoId = reserva.PedidoId,
                Sku = reserva.Sku,
                Cantidad = reserva.Cantidad,
                Motivo = reserva.Motivo ?? string.Empty,
                OcurridoEn = reserva.CreadoEn
            };
    }
}
