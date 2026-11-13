using CapaDatos.Interfaz.pedidos.interfaz;
using CapaDatos.Interfaz.productos.interfaz;
using CapaDto.Modelos;
using CapaDto.Peticiones;
using CapaDto.Respuestas;
using CapaNegocios.Implementacion.pedidos.Implementacion;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderFlow.Contratos.Pedidos;
using System.Text.Json;

namespace Orders_Tests
{
    public class CrearPedidoTests
    {
        private readonly Mock<IPedidosCapaDatos> _pedidosCapaDatos = new();
        private readonly Mock<IProductosCapaDatos> _productosCapaDatos = new();
        private readonly clsPedidosCapaNegocios _negocios;

        public CrearPedidoTests()
        {
            // Catálogo simulado: solo ABC-01 existe.
            _productosCapaDatos.Setup(p => p.ExisteActivo(It.IsAny<string>())).ReturnsAsync(false);
            _productosCapaDatos.Setup(p => p.ExisteActivo("ABC-01")).ReturnsAsync(true);

            _negocios = new clsPedidosCapaNegocios(
                _pedidosCapaDatos.Object, _productosCapaDatos.Object, NullLogger<clsPedidosCapaNegocios>.Instance);
        }

        [Theory]
        [InlineData("", "ABC-01", 1, "clienteNombre")]
        [InlineData("   ", "ABC-01", 1, "clienteNombre")]
        [InlineData("Ana", "XYZ-99", 1, "sku")]
        [InlineData("Ana", "ABC-01", 0, "cantidad")]
        [InlineData("Ana", "ABC-01", 101, "cantidad")]
        public async Task Crear_ConDatosInvalidos_DevuelveErrorEnElCampoYNoGuarda(string nombre, string sku, int cantidad, string campoConError)
        {
            var request = new CrearPedidoRequest { ClienteNombre = nombre, Sku = sku, Cantidad = cantidad };

            ResultadoPedido<PedidoDto> resultado = await _negocios.Crear(request);

            Assert.Equal(EstadoResultado.DatosInvalidos, resultado.Estado);
            Assert.Equal(new[] { campoConError }, resultado.Errores.Keys);
            _pedidosCapaDatos.Verify(p => p.CrearPedido(It.IsAny<NuevoPedido>(), It.IsAny<EventoOutbox>()), Times.Never);
        }

        [Fact]
        public async Task Crear_ConDatosValidos_GuardaEnPendingConSuEventoPedidoCreadoEnElOutbox()
        {
            NuevoPedido? pedidoGuardado = null;
            EventoOutbox? eventoGuardado = null;
            _pedidosCapaDatos
                .Setup(p => p.CrearPedido(It.IsAny<NuevoPedido>(), It.IsAny<EventoOutbox>()))
                .Callback<NuevoPedido, EventoOutbox>((pedido, evento) => { pedidoGuardado = pedido; eventoGuardado = evento; })
                .Returns(Task.CompletedTask);

            var request = new CrearPedidoRequest { ClienteNombre = "  Ana Pérez ", Sku = "ABC-01", Cantidad = 2 };

            ResultadoPedido<PedidoDto> resultado = await _negocios.Crear(request);

            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoPedido.Pending, resultado.Datos!.Estado);
            Assert.Equal("Ana Pérez", resultado.Datos.ClienteNombre);

            // Pedido y evento van juntos a la capa de datos (misma transacción) y describen el mismo pedido.
            Assert.NotNull(pedidoGuardado);
            Assert.NotNull(eventoGuardado);
            Assert.Equal(nameof(PedidoCreado), eventoGuardado!.Tipo);
            PedidoCreado evento = JsonSerializer.Deserialize<PedidoCreado>(eventoGuardado.Contenido)!;
            Assert.Equal(pedidoGuardado!.PedidoId, evento.PedidoId);
            Assert.Equal(eventoGuardado.OutboxId, evento.EventoId);
            Assert.Equal("ABC-01", evento.Sku);
            Assert.Equal(2, evento.Cantidad);
        }
    }
}
