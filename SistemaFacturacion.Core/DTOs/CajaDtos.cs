using System;

namespace SistemaFacturacion.Core.DTOs
{
    public class AperturaCajaDto
    {
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public decimal MontoInicial { get; set; }
    }

    public class CierreCajaDto
    {
        public int CajaSesionId { get; set; }
        public decimal EfectivoReal { get; set; }
        public string? NotasCierre { get; set; }
    }

    public class MovimientoCajaDto
    {
        public int CajaSesionId { get; set; }
        public string Tipo { get; set; } = "Ingreso"; // Ingreso, Egreso
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class ResumenCajaDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
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
        public string Estado { get; set; } = "Abierta";
        public string? NotasCierre { get; set; }
    }
}
