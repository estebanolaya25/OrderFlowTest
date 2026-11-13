using CapaNegocios.Interfaz.reservas.interfaz;
using CapaNegocios.util;
using MassTransit;
using OrderFlow.Contratos.Pedidos;

namespace Inventory_Api.mensajeria
{
    // Traduce el mensaje de RabbitMQ, delega en la capa de negocios y publica la respuesta.
    //
    // ¿Qué pasa si la publicación falla después de guardar la reserva? La excepción hace que MassTransit
    // reintente el mismo mensaje; esta vez la capa de datos lo detecta como duplicado, no toca el stock
    // y devuelve el resultado guardado, que se vuelve a publicar con el mismo EventoId.
    public class PedidoCreadoConsumer : IConsumer<PedidoCreado>
    {
        private readonly IReservasCapaNegocios _reservasCapaNegocios;

        public PedidoCreadoConsumer(IReservasCapaNegocios reservasCapaNegocios) => _reservasCapaNegocios = reservasCapaNegocios;

        public async Task Consume(ConsumeContext<PedidoCreado> context)
        {
            RespuestaReserva respuesta = await _reservasCapaNegocios.ProcesarPedidoCreado(context.Message);

            await context.Publish(respuesta.Evento, respuesta.Evento.GetType(),
                Pipe.Execute<PublishContext>(publicacion => publicacion.MessageId = respuesta.EventoId));
        }
    }
}
