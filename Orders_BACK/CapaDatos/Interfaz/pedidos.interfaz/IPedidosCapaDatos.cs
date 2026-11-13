using CapaDto.Modelos;
using CapaDto.Respuestas;

namespace CapaDatos.Interfaz.pedidos.interfaz
{
    public interface IPedidosCapaDatos
    {
        // Guarda el pedido (Pending) y su evento en el Outbox en una sola transacción.
        Task CrearPedido(NuevoPedido pedido, EventoOutbox evento);

        Task<PedidoDto?> ObtenerPedido(Guid pedidoId);

        Task<List<PedidoDto>> ListarPedidos(int limite);

        // Registra el mensaje como procesado y cambia el estado si el pedido está en un estado permitido, todo atómico.
        Task<ResultadoTransicion> AplicarTransicion(TransicionPedido transicion);
    }
}
