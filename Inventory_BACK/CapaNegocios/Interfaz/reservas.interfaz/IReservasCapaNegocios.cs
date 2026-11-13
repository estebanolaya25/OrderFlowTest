using CapaNegocios.util;
using OrderFlow.Contratos.Pedidos;

namespace CapaNegocios.Interfaz.reservas.interfaz
{
    public interface IReservasCapaNegocios
    {
        // Procesa el pedido y devuelve el evento que hay que publicar (StockReservado o StockRechazado).
        // La capa de negocios no conoce RabbitMQ: publicar es responsabilidad del consumidor.
        Task<RespuestaReserva> ProcesarPedidoCreado(PedidoCreado evento);
    }
}
