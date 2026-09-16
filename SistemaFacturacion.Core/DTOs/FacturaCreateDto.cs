using System.Collections.Generic;

namespace SistemaFacturacion.Core.DTOs
{
    public class FacturaCreateDto
    {
        public int ClienteId { get; set; }
        public int ImpuestoId { get; set; } = 1; // Por defecto IVA 19%
        public string FormaPago { get; set; } = "Efectivo";
        public decimal PagaCon { get; set; } = 0;
        public decimal Cambio { get; set; } = 0;
        public int? CajaSesionId { get; set; }
        public decimal DescuentoPorcentaje { get; set; } = 0;
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public List<DetalleFacturaCreateDto> Detalles { get; set; } = new();
    }

    public class DetalleFacturaCreateDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
