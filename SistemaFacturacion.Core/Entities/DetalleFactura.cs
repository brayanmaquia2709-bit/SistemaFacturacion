namespace SistemaFacturacion.Core.Entities
{
    public class DetalleFactura
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ImpuestoMonto { get; set; }

        // Propiedades de navegación
        public Factura? Factura { get; set; }
        public Producto? Producto { get; set; }
    }
}
