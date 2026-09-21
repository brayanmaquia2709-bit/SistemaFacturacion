namespace SistemaFacturacion.Core.Entities
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string CodigoBarra { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal PrecioCOP => Precio < 10000 ? Precio * 1000 : Precio;
        public string PrecioCOPFormatted => $"$ {PrecioCOP:N0} COP";
    }
}
