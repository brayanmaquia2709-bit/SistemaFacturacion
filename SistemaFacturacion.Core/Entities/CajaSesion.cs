using System;
using System.Collections.Generic;

namespace SistemaFacturacion.Core.Entities
{
    public class CajaSesion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasTarjeta { get; set; }
        public decimal TotalVentasTransferencia { get; set; }
        public decimal TotalVentasCredito { get; set; }
        public decimal TotalAbonos { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal EfectivoEsperado { get; set; }
        public decimal EfectivoReal { get; set; }
        public decimal Diferencia { get; set; }
        public string Estado { get; set; } = "Abierta"; // Abierta, Cerrada
        public string? NotasCierre { get; set; }

        public List<MovimientoCaja> Movimientos { get; set; } = new();
    }
}
