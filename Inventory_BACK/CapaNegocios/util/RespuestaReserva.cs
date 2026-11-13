namespace CapaNegocios.util
{
    public class RespuestaReserva
    {
        // StockReservado o StockRechazado, listo para publicar.
        public object Evento { get; init; } = default!;

        // Se usa como MessageId al publicar: es el mismo en cada republicación del mismo pedido.
        public Guid EventoId { get; init; }

        // true: el pedido ya se había procesado; el stock no se tocó y se republica el resultado original.
        public bool Duplicado { get; init; }
    }
}
