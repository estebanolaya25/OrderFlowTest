using CapaDatos.Interfaz.productos.interfaz;
using CapaDto.Respuestas;
using CapaNegocios.Interfaz.productos.interfaz;

namespace CapaNegocios.Implementacion.productos.Implementacion
{
    public class clsProductosCapaNegocios : IProductosCapaNegocios
    {
        private readonly IProductosCapaDatos _productosCapaDatos;

        public clsProductosCapaNegocios(IProductosCapaDatos productosCapaDatos)
        {
            _productosCapaDatos = productosCapaDatos;
        }

        public Task<List<ProductoDto>> Listar() => _productosCapaDatos.ListarActivos();
    }
}
