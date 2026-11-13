using CapaDto.Respuestas;

namespace CapaNegocios.Interfaz.stock.interfaz
{
    public interface IStockCapaNegocios
    {
        Task<List<StockDto>> Listar();
    }
}
