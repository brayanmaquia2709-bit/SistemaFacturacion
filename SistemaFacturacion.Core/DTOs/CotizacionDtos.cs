using System.Collections.Generic;

namespace SistemaFacturacion.Core.DTOs
{
    public class CotizacionCreateDto
    {
        public int ClienteId { get; set; }
        public string? Notas { get; set; }
        public List<DetalleCotizacionCreateDto> Detalles { get; set; } = new();
    }

    public class DetalleCotizacionCreateDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
