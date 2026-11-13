using System.Text.Json;

namespace CapaDto.Modelos
{
    public static class EstadoPedido
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Rejected = "Rejected";
    }

    public class NuevoPedido
    {
        public Guid PedidoId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public DateTime CreadoEn { get; set; }
    }

    // Cambio de estado atómico: solo se aplica si el pedido está en uno de los estados permitidos.
    // MensajeId se registra en la misma transacción para descartar eventos repetidos.
    public class TransicionPedido
    {
        public Guid PedidoId { get; set; }
        public string[] EstadosPermitidos { get; set; } = Array.Empty<string>();
        public string NuevoEstado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public Guid MensajeId { get; set; }
        public string TipoMensaje { get; set; } = string.Empty;
    }

    public enum ResultadoTransicion
    {
        Aplicada,
        EstadoNoPermitido,
        NoEncontrado,
        MensajeDuplicado
    }

    public class EventoOutbox
    {
        public Guid OutboxId { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public string Contenido { get; init; } = string.Empty;

        // El OutboxId es el EventoId: se usa también como MessageId al publicar en RabbitMQ.
        public static EventoOutbox Crear<T>(Guid eventoId, T evento) where T : class => new()
        {
            OutboxId = eventoId,
            Tipo = typeof(T).Name,
            Contenido = JsonSerializer.Serialize(evento)
        };
    }

    public class MensajeOutbox
    {
        public Guid OutboxId { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public string Contenido { get; init; } = string.Empty;
    }
}
