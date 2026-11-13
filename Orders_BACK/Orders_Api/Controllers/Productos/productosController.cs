using CapaDto.Respuestas;
using CapaNegocios.Interfaz.productos.interfaz;
using Microsoft.AspNetCore.Mvc;

namespace Orders_Api.Controllers.Productos
{
    [Route("products")]
    [ApiController]
    [Produces("application/json")]
    public class productosController : ControllerBase
    {
        private readonly IProductosCapaNegocios _productosCapaNegocios;

        public productosController(IProductosCapaNegocios productosCapaNegocios)
        {
            _productosCapaNegocios = productosCapaNegocios;
        }

        /// <summary>Catálogo de SKU que se pueden pedir (alimenta el selector del formulario).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar() => Ok(await _productosCapaNegocios.Listar());
    }
}
