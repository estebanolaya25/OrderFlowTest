using CapaDatos.Interfaz.stock.interfaz;
using CapaDto.Respuestas;
using CapaNegocios.Interfaz.stock.interfaz;

namespace CapaNegocios.Implementacion.stock.Implementacion
{
    public class clsStockCapaNegocios : IStockCapaNegocios
    {
        private readonly IStockCapaDatos _stockCapaDatos;

        public clsStockCapaNegocios(IStockCapaDatos stockCapaDatos)
        {
            _stockCapaDatos = stockCapaDatos;
        }

        public Task<List<StockDto>> Listar() => _stockCapaDatos.Listar();
    }
}
