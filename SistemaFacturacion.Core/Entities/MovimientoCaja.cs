using System;

namespace SistemaFacturacion.Core.Entities
{
    public class MovimientoCaja
    {
        public int Id { get; set; }
        public int CajaSesionId { get; set; }
        public string Tipo { get; set; } = "Ingreso"; // Ingreso, Egreso
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }
}
