using CapaDto.Respuestas;

namespace CapaDatos.Interfaz.productos.interfaz
{
    public interface IProductosCapaDatos
    {
        Task<List<ProductoDto>> ListarActivos();

        Task<bool> ExisteActivo(string sku);
    }
}
