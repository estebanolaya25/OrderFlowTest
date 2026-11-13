using CapaDto.Peticiones;

namespace CapaNegocios.util
{
    // Reglas del pedido que no necesitan base de datos. Que el SKU exista en el catálogo
    // lo comprueba clsPedidosCapaNegocios después, solo si el formato ya es válido.
    // Las claves del diccionario son los nombres de los campos en el JSON (camelCase), para que el front los ubique.
    public static class clsValidadorPedido
    {
        public const int CantidadMinima = 1;
        public const int CantidadMaxima = 100;
        public const int LongitudMaximaNombre = 150;
        public const int LongitudMaximaSku = 50;

        public const string CampoClienteNombre = "clienteNombre";
        public const string CampoSku = "sku";
        public const string CampoCantidad = "cantidad";

        public static Dictionary<string, string[]> Validar(CrearPedidoRequest request)
        {
            var errores = new Dictionary<string, string[]>();

            string? nombre = request.ClienteNombre?.Trim();
            if (string.IsNullOrEmpty(nombre))
                errores[CampoClienteNombre] = new[] { "El nombre del cliente es obligatorio." };
            else if (nombre.Length > LongitudMaximaNombre)
                errores[CampoClienteNombre] = new[] { $"El nombre del cliente no puede superar {LongitudMaximaNombre} caracteres." };

            string? sku = request.Sku?.Trim();
            if (string.IsNullOrEmpty(sku))
                errores[CampoSku] = new[] { "El SKU es obligatorio." };
            else if (sku.Length > LongitudMaximaSku)
                errores[CampoSku] = new[] { $"El SKU no puede superar {LongitudMaximaSku} caracteres." };

            if (request.Cantidad is null)
                errores[CampoCantidad] = new[] { "La cantidad es obligatoria." };
            else if (request.Cantidad < CantidadMinima || request.Cantidad > CantidadMaxima)
                errores[CampoCantidad] = new[] { $"La cantidad debe estar entre {CantidadMinima} y {CantidadMaxima}." };

            return errores;
        }
    }
}
