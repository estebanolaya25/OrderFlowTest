using CapaDto.Modelos;
using CapaNegocios.util;

namespace Orders_Tests
{
    public class TransicionesPedidoTests
    {
        // Solo se sale de Pending. Confirmed y Rejected son finales: un evento tardío o repetido
        // (por ejemplo un StockRechazado después de un StockReservado) no puede cambiarlos.
        [Theory]
        [InlineData(EstadoPedido.Pending, EstadoPedido.Confirmed, true)]
        [InlineData(EstadoPedido.Pending, EstadoPedido.Rejected, true)]
        [InlineData(EstadoPedido.Confirmed, EstadoPedido.Rejected, false)]
        [InlineData(EstadoPedido.Rejected, EstadoPedido.Confirmed, false)]
        [InlineData(EstadoPedido.Confirmed, EstadoPedido.Confirmed, false)]
        [InlineData(EstadoPedido.Confirmed, EstadoPedido.Pending, false)]
        public void EsValida_SoloPermiteSalirDePending(string estadoActual, string nuevoEstado, bool esperado)
        {
            Assert.Equal(esperado, clsTransicionesPedido.EsValida(estadoActual, nuevoEstado));
        }
    }
}
