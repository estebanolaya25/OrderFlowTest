namespace OrderFlow.Contratos.Pedidos
{
    // Contratos entre Orders e Inventory. Inventory_BACK/CapaDTO/Contratos tiene una copia idéntica:
    // MassTransit enruta por el nombre completo del tipo, así que namespace y nombres deben coincidir.
    // EventoId identifica el evento: si llega dos veces, el consumidor lo reconoce como duplicado.

    // Publicado por Orders al crear un pedido.
    public record PedidoCreado
    {
        public Guid EventoId { get; init; }
        public Guid PedidoId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public int Cantidad { get; init; }
        public DateTime OcurridoEn { get; init; }
    }

    // Publicado por Inventory cuando descontó el stock.
    public record StockReservado
    {
        public Guid EventoId { get; init; }
        public Guid PedidoId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public int Cantidad { get; init; }
        public DateTime OcurridoEn { get; init; }
    }

    // Publicado por Inventory cuando no hay stock suficiente (o el SKU no existe en inventario).
    public record StockRechazado
    {
        public Guid EventoId { get; init; }
        public Guid PedidoId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public int Cantidad { get; init; }
        public string Motivo { get; init; } = string.Empty;
        public DateTime OcurridoEn { get; init; }
    }
}
