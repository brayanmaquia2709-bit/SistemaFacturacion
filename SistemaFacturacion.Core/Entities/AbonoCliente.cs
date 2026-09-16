using System;

namespace SistemaFacturacion.Core.Entities
{
    public class AbonoCliente
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public int? FacturaId { get; set; }
        public string? NumeroFactura { get; set; }
        public int? CajaSesionId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string FormaPago { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }

        public Cliente? Cliente { get; set; }
        public Factura? Factura { get; set; }
    }
}
