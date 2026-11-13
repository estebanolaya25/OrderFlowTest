using CapaNegocios.Interfaz.pedidos.interfaz;
using MassTransit;
using OrderFlow.Contratos.Pedidos;

namespace Orders_Api.mensajeria
{
    // Los consumidores solo traducen el mensaje de RabbitMQ y delegan en la capa de negocios.
    // Si la base de datos falla, la excepción sube y MassTransit reintenta (UseMessageRetry en Program.cs).
    public class StockReservadoConsumer : IConsumer<StockReservado>
    {
        private readonly IPedidosCapaNegocios _pedidosCapaNegocios;

        public StockReservadoConsumer(IPedidosCapaNegocios pedidosCapaNegocios) => _pedidosCapaNegocios = pedidosCapaNegocios;

        public async Task Consume(ConsumeContext<StockReservado> context) =>
            await _pedidosCapaNegocios.ProcesarStockReservado(context.Message);
    }

    public class StockRechazadoConsumer : IConsumer<StockRechazado>
    {
        private readonly IPedidosCapaNegocios _pedidosCapaNegocios;

        public StockRechazadoConsumer(IPedidosCapaNegocios pedidosCapaNegocios) => _pedidosCapaNegocios = pedidosCapaNegocios;

        public async Task Consume(ConsumeContext<StockRechazado> context) =>
            await _pedidosCapaNegocios.ProcesarStockRechazado(context.Message);
    }
}
