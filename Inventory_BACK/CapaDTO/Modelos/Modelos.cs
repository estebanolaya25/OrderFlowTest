namespace CapaDto.Modelos
{
    public static class ResultadoReserva
    {
        public const string Reservado = "Reservado";
        public const string Rechazado = "Rechazado";
    }

    public static class MotivosRechazo
    {
        public static string SkuInexistente(string sku) => $"El SKU '{sku}' no existe en inventario.";

        public static string StockInsuficiente(int solicitado, int disponible) =>
            $"Stock insuficiente: se pidieron {solicitado} y hay {disponible} disponibles.";
    }

    // Lo que la capa de negocios pide a la capa de datos al recibir un PedidoCreado.
    public class SolicitudReserva
    {
        public Guid MensajeId { get; set; }
        public Guid PedidoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int Cantidad { get; set; }

        // Id que tendrá el evento de respuesta si esta es la primera vez que se procesa el pedido.
        public Guid EventoRespuestaId { get; set; }
    }

    // Fila de inv.Reservas: el resultado definitivo de un pedido.
    public class Reserva
    {
        public Guid PedidoId { get; set; }
        public Guid EventoRespuestaId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public DateTime CreadoEn { get; set; }
    }

    public class ResultadoProcesamiento
    {
        // true: el evento (o el pedido) ya se había procesado; el stock no se tocó y Reserva es el resultado original.
        public bool Duplicado { get; init; }
        public Reserva Reserva { get; init; } = new();
    }
}
