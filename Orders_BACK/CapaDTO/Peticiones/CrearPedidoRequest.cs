namespace CapaDto.Peticiones
{
    // Campos anulables a propósito: la validación la hace la capa de negocios y devuelve
    // un error por campo, en vez de que un valor faltante se convierta silenciosamente en "" o 0.
    public class CrearPedidoRequest
    {
        public string? ClienteNombre { get; set; }
        public string? Sku { get; set; }
        public int? Cantidad { get; set; }
    }
}
