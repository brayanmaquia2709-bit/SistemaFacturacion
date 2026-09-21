using System;
using System.Collections.Generic;

namespace SistemaFacturacion.Core.Entities
{
    public class Factura
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal DescuentoMonto { get; set; }
        public decimal ImpuestosTotal { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = "Efectivo"; // Efectivo, TarjetaCredito, TarjetaDebito, Transferencia, Credito
        public string Estado { get; set; } = "Emitida"; // Emitida, Anulada
        public string EstadoPago { get; set; } = "Pagada"; // Pagada, Pendiente, Parcial
        public decimal PagaCon { get; set; }
        public decimal Cambio { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int? CajaSesionId { get; set; }
        public string? Notas { get; set; }

        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }

        // Propiedad de navegación
        public Cliente? Cliente { get; set; }
        public List<DetalleFactura> Detalles { get; set; } = new();

        public decimal TotalCOP => Total < 10000 ? Total * 1000 : Total;
        public string TotalCOPFormatted => $"$ {TotalCOP:N0} COP";
        public string ClienteNombreMostrar => Cliente?.Nombre ?? (!string.IsNullOrWhiteSpace(UsuarioNombre) ? $"Cliente General ({UsuarioNombre})" : "Cliente General");
    }
}
