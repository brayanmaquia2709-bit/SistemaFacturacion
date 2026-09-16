using System;

namespace SistemaFacturacion.Core.Entities
{
    public class NotaCredito
    {
        public int Id { get; set; }
        public string NumeroNota { get; set; } = string.Empty;
        public int FacturaId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Motivo { get; set; } = "Anulación de factura";
        public decimal MontoTotal { get; set; }

        public Factura? Factura { get; set; }
    }
}
