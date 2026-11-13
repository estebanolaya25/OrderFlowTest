using CapaDto.Modelos;

namespace CapaDatos.Interfaz.reservas.interfaz
{
    public interface IReservasCapaDatos
    {
        // En una sola transacción: registra el mensaje como procesado, descuenta el stock si alcanza
        // y guarda el resultado en inv.Reservas. Si el mensaje o el pedido ya se habían procesado,
        // no toca el stock y devuelve el resultado original con Duplicado = true.
        Task<ResultadoProcesamiento> Procesar(SolicitudReserva solicitud);
    }
}
