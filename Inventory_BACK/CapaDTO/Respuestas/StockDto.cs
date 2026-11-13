namespace CapaDto.Respuestas
{
    public class StockDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Disponible { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }
}
