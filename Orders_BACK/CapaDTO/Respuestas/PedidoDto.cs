namespace CapaDto.Respuestas
{
    public class PedidoDto
    {
        public Guid Id { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }

    public class ProductoDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }
}
