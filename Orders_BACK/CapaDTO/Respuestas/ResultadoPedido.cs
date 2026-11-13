namespace CapaDto.Respuestas
{
    public enum EstadoResultado
    {
        Exitoso,
        DatosInvalidos,
        NoEncontrado
    }

    public class ResultadoPedido<T>
    {
        public EstadoResultado Estado { get; private set; }
        public string? Mensaje { get; private set; }
        public T? Datos { get; private set; }

        // Errores por campo (clave = nombre del campo en el JSON), para que el front los muestre junto a cada input.
        public Dictionary<string, string[]> Errores { get; private set; } = new();

        public bool Exitoso => Estado == EstadoResultado.Exitoso;

        public static ResultadoPedido<T> Ok(T datos) => new() { Estado = EstadoResultado.Exitoso, Datos = datos };

        public static ResultadoPedido<T> Invalido(Dictionary<string, string[]> errores) => new()
        {
            Estado = EstadoResultado.DatosInvalidos,
            Mensaje = "Los datos del pedido no son válidos.",
            Errores = errores
        };

        public static ResultadoPedido<T> NoEncontrado(string mensaje) => new() { Estado = EstadoResultado.NoEncontrado, Mensaje = mensaje };
    }
}
