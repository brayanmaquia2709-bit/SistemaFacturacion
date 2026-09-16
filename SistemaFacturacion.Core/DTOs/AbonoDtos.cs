using System;

namespace SistemaFacturacion.Core.DTOs
{
    public class AbonoCreateDto
    {
        public int ClienteId { get; set; }
        public int? FacturaId { get; set; }
        public int? CajaSesionId { get; set; }
        public decimal Monto { get; set; }
        public string FormaPago { get; set; } = "Efectivo";
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CuentaPorCobrarDto
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public decimal LimiteCredito { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int FacturasPendientesCount { get; set; }
    }
}
