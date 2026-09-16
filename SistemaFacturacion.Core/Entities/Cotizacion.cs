using System;
using System.Collections.Generic;

namespace SistemaFacturacion.Core.Entities
{
    public class Cotizacion
    {
        public int Id { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime FechaVencimiento { get; set; } = DateTime.Now.AddDays(15);
        public decimal Subtotal { get; set; }
        public decimal ImpuestosTotal { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Convertida, Rechazada
        public string? Notas { get; set; }

        public Cliente? Cliente { get; set; }
        public List<DetalleCotizacion> Detalles { get; set; } = new();
    }
}
