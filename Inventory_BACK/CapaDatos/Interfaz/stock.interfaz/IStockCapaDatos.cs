using CapaDto.Respuestas;

namespace CapaDatos.Interfaz.stock.interfaz
{
    public interface IStockCapaDatos
    {
        Task<List<StockDto>> Listar();
    }
}
