using CapaDatos.Interfaz.pedidos.interfaz;
using CapaDatos.Interfaz.productos.interfaz;
using CapaDto.Modelos;
using CapaDto.Peticiones;
using CapaDto.Respuestas;
using CapaNegocios.Interfaz.pedidos.interfaz;
using CapaNegocios.util;
using Microsoft.Extensions.Logging;
using OrderFlow.Contratos.Pedidos;

namespace CapaNegocios.Implementacion.pedidos.Implementacion
{
    public class clsPedidosCapaNegocios : IPedidosCapaNegocios
    {
        // Sin paginación a propósito: el panel muestra los pedidos más recientes. Documentado en el README.
        public const int LimiteListado = 200;

        private readonly IPedidosCapaDatos _pedidosCapaDatos;
        private readonly IProductosCapaDatos _productosCapaDatos;
        private readonly ILogger<clsPedidosCapaNegocios> _logger;

        public clsPedidosCapaNegocios(
            IPedidosCapaDatos pedidosCapaDatos,
            IProductosCapaDatos productosCapaDatos,
            ILogger<clsPedidosCapaNegocios> logger)
        {
            _pedidosCapaDatos = pedidosCapaDatos;
            _productosCapaDatos = productosCapaDatos;
            _logger = logger;
        }

        public async Task<ResultadoPedido<PedidoDto>> Crear(CrearPedidoRequest request)
        {
            Dictionary<string, string[]> errores = clsValidadorPedido.Validar(request);

            // Solo se consulta el catálogo si el SKU tiene un formato válido.
            if (!errores.ContainsKey(clsValidadorPedido.CampoSku) && !await _productosCapaDatos.ExisteActivo(request.Sku!.Trim()))
                errores[clsValidadorPedido.CampoSku] = new[] { $"El SKU '{request.Sku!.Trim()}' no existe en el catálogo." };

            if (errores.Count > 0)
                return ResultadoPedido<PedidoDto>.Invalido(errores);

            var pedido = new NuevoPedido
            {
                PedidoId = Guid.NewGuid(),
                ClienteNombre = request.ClienteNombre!.Trim(),
                Sku = request.Sku!.Trim(),
                Cantidad = request.Cantidad!.Value,
                CreadoEn = DateTime.UtcNow
            };

            var evento = new PedidoCreado
            {
                EventoId = Guid.NewGuid(),
                PedidoId = pedido.PedidoId,
                Sku = pedido.Sku,
                Cantidad = pedido.Cantidad,
                OcurridoEn = pedido.CreadoEn
            };

            // El evento no se publica aquí: queda en el Outbox y PublicadorOutboxService lo envía.
            // Así el pedido se crea aunque RabbitMQ esté caído.
            await _pedidosCapaDatos.CrearPedido(pedido, EventoOutbox.Crear(evento.EventoId, evento));
            _logger.LogInformation("Pedido {PedidoId} creado en Pending ({Sku} x {Cantidad}).", pedido.PedidoId, pedido.Sku, pedido.Cantidad);

            return ResultadoPedido<PedidoDto>.Ok(new PedidoDto
            {
                Id = pedido.PedidoId,
                ClienteNombre = pedido.ClienteNombre,
                Sku = pedido.Sku,
                Cantidad = pedido.Cantidad,
                Estado = EstadoPedido.Pending,
                CreadoEn = pedido.CreadoEn,
                ActualizadoEn = pedido.CreadoEn
            });
        }

        public Task<PedidoDto?> Obtener(Guid pedidoId) => _pedidosCapaDatos.ObtenerPedido(pedidoId);

        public Task<List<PedidoDto>> Listar() => _pedidosCapaDatos.ListarPedidos(LimiteListado);

        public Task<ResultadoTransicion> ProcesarStockReservado(StockReservado evento) =>
            AplicarResultadoStock(evento.EventoId, nameof(StockReservado), evento.PedidoId, EstadoPedido.Confirmed, motivo: null);

        public Task<ResultadoTransicion> ProcesarStockRechazado(StockRechazado evento) =>
            AplicarResultadoStock(evento.EventoId, nameof(StockRechazado), evento.PedidoId, EstadoPedido.Rejected, evento.Motivo);

        private async Task<ResultadoTransicion> AplicarResultadoStock(Guid eventoId, string tipo, Guid pedidoId, string nuevoEstado, string? motivo)
        {
            ResultadoTransicion resultado = await _pedidosCapaDatos.AplicarTransicion(new TransicionPedido
            {
                PedidoId = pedidoId,
                EstadosPermitidos = clsTransicionesPedido.EstadosOrigen(nuevoEstado),
                NuevoEstado = nuevoEstado,
                Motivo = motivo,
                MensajeId = eventoId,
                TipoMensaje = tipo
            });

            // Ningún caso lanza excepción: un duplicado o un evento tardío no deben reintentarse ni ir a la cola de error.
            switch (resultado)
            {
                case ResultadoTransicion.Aplicada:
                    _logger.LogInformation("Pedido {PedidoId} pasó a {Estado}.", pedidoId, nuevoEstado);
                    break;
                case ResultadoTransicion.MensajeDuplicado:
                    _logger.LogInformation("{Tipo} {EventoId} ya fue procesado; se ignora.", tipo, eventoId);
                    break;
                case ResultadoTransicion.EstadoNoPermitido:
                    _logger.LogWarning("{Tipo} para el pedido {PedidoId}: ya no está en Pending; se ignora.", tipo, pedidoId);
                    break;
                case ResultadoTransicion.NoEncontrado:
                    _logger.LogWarning("{Tipo} para un pedido inexistente {PedidoId}; se ignora.", tipo, pedidoId);
                    break;
            }

            return resultado;
        }
    }
}
