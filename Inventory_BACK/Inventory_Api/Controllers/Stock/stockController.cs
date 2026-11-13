using CapaDto.Respuestas;
using CapaNegocios.Interfaz.stock.interfaz;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Api.Controllers.Stock
{
    [Route("stock")]
    [ApiController]
    [Produces("application/json")]
    public class stockController : ControllerBase
    {
        private readonly IStockCapaNegocios _stockCapaNegocios;

        public stockController(IStockCapaNegocios stockCapaNegocios)
        {
            _stockCapaNegocios = stockCapaNegocios;
        }

        /// <summary>Stock disponible por SKU (solo lectura; útil para comprobar la idempotencia).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<StockDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar() => Ok(await _stockCapaNegocios.Listar());
    }
}
