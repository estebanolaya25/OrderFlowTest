using CapaDto.Modelos;
using CapaDto.Peticiones;
using CapaDto.Respuestas;
using OrderFlow.Contratos.Pedidos;

namespace CapaNegocios.Interfaz.pedidos.interfaz
{
    public interface IPedidosCapaNegocios
    {
        Task<ResultadoPedido<PedidoDto>> Crear(CrearPedidoRequest request);

        Task<PedidoDto?> Obtener(Guid pedidoId);

        Task<List<PedidoDto>> Listar();

        // Reacción a los eventos de Inventory: Pending → Confirmed / Rejected.
        Task<ResultadoTransicion> ProcesarStockReservado(StockReservado evento);

        Task<ResultadoTransicion> ProcesarStockRechazado(StockRechazado evento);
    }
}
