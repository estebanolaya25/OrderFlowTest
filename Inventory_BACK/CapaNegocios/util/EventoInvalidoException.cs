namespace CapaNegocios.util
{
    // Un evento con datos imposibles (cantidad <= 0, SKU vacío) no se arregla reintentando:
    // Program.cs la excluye de los reintentos y el mensaje va directo a la cola de error.
    public class EventoInvalidoException : Exception
    {
        public EventoInvalidoException(string mensaje) : base(mensaje) { }
    }
}
