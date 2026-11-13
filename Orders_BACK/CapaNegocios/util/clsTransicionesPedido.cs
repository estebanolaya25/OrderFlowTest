using CapaDto.Modelos;

namespace CapaNegocios.util
{
    // Máquina de estados del pedido:
    //   Pending ──► Confirmed
    //   Pending ──► Rejected
    // Confirmed y Rejected son finales: un evento tardío o repetido no puede cambiarlos.
    public static class clsTransicionesPedido
    {
        private static readonly Dictionary<string, string[]> OrigenesPermitidos = new()
        {
            [EstadoPedido.Confirmed] = new[] { EstadoPedido.Pending },
            [EstadoPedido.Rejected] = new[] { EstadoPedido.Pending }
        };

        // Estados desde los que se puede llegar a nuevoEstado; la capa de datos los usa en el WHERE del UPDATE.
        public static string[] EstadosOrigen(string nuevoEstado) =>
            OrigenesPermitidos.TryGetValue(nuevoEstado, out string[]? origenes) ? origenes : Array.Empty<string>();

        public static bool EsValida(string estadoActual, string nuevoEstado) => EstadosOrigen(nuevoEstado).Contains(estadoActual);
    }
}
