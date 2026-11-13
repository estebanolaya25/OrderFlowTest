using CapaDatos.Interfaz.reservas.interfaz;
using CapaDto.Modelos;
using CapaNegocios.Implementacion.reservas.Implementacion;
using CapaNegocios.util;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderFlow.Contratos.Pedidos;

namespace Inventory_Tests
{
    public class EventoInvalidoTests
    {
        // Una cantidad negativa es peligrosa: "Disponible - (-5)" sumaría stock. El evento se rechaza
        // antes de llegar a la base de datos y (por EventoInvalidoException) no se reintenta.
        [Theory]
        [InlineData("ABC-01", 0)]
        [InlineData("ABC-01", -5)]
        [InlineData("", 1)]
        public async Task ProcesarPedidoCreado_ConDatosInvalidos_NoTocaElStock(string sku, int cantidad)
        {
            var reservasCapaDatos = new Mock<IReservasCapaDatos>();
            var negocios = new clsReservasCapaNegocios(reservasCapaDatos.Object, NullLogger<clsReservasCapaNegocios>.Instance);
            var evento = new PedidoCreado { EventoId = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Sku = sku, Cantidad = cantidad };

            await Assert.ThrowsAsync<EventoInvalidoException>(() => negocios.ProcesarPedidoCreado(evento));

            reservasCapaDatos.Verify(r => r.Procesar(It.IsAny<SolicitudReserva>()), Times.Never);
        }
    }
}
