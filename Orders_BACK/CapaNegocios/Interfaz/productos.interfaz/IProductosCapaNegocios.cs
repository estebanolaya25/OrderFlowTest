using CapaDto.Respuestas;

namespace CapaNegocios.Interfaz.productos.interfaz
{
    public interface IProductosCapaNegocios
    {
        Task<List<ProductoDto>> Listar();
    }
}
