using CapaDto.Peticiones;
using CapaDto.Respuestas;
using CapaNegocios.Interfaz.pedidos.interfaz;
using Microsoft.AspNetCore.Mvc;

namespace Orders_Api.Controllers.Pedidos
{
    [Route("orders")]
    [ApiController]
    [Produces("application/json")]
    public class pedidosController : ControllerBase
    {
        private readonly IPedidosCapaNegocios _pedidosCapaNegocios;

        public pedidosController(IPedidosCapaNegocios pedidosCapaNegocios)
        {
            _pedidosCapaNegocios = pedidosCapaNegocios;
        }

        /// <summary>Crea un pedido en estado Pending y encola el evento PedidoCreado.</summary>
        /// <response code="201">Pedido creado.</response>
        /// <response code="400">Datos inválidos; el detalle viene por campo en "errors".</response>
        [HttpPost]
        [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear(CrearPedidoRequest request)
        {
            ResultadoPedido<PedidoDto> resultado = await _pedidosCapaNegocios.Crear(request);
            if (!resultado.Exitoso)
                return MapearError(resultado);

            return CreatedAtAction(nameof(Obtener), new { id = resultado.Datos!.Id }, resultado.Datos);
        }

        /// <summary>Lista los pedidos más recientes con su estado actual.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PedidoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar() => Ok(await _pedidosCapaNegocios.Listar());

        /// <summary>Detalle de un pedido.</summary>
        /// <response code="404">El pedido no existe.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obtener(Guid id)
        {
            PedidoDto? pedido = await _pedidosCapaNegocios.Obtener(id);
            return pedido == null
                ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Pedido no encontrado.")
                : Ok(pedido);
        }

        // Los errores salen como ProblemDetails (RFC 7807), el mismo formato que usa ASP.NET
        // cuando el JSON llega mal formado: el front maneja un solo formato de error.
        private IActionResult MapearError<T>(ResultadoPedido<T> resultado) => resultado.Estado switch
        {
            EstadoResultado.DatosInvalidos => ValidationProblem(new ValidationProblemDetails(resultado.Errores) { Title = resultado.Mensaje }),
            EstadoResultado.NoEncontrado => Problem(statusCode: StatusCodes.Status404NotFound, title: resultado.Mensaje),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: resultado.Mensaje)
        };
    }
}
