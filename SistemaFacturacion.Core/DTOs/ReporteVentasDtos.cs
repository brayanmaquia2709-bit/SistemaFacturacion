namespace SistemaFacturacion.Core.DTOs
{
    public class ReporteVentasMesDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public int TotalFacturas { get; set; }
        public decimal TotalVentas { get; set; }
    }

    public class ReporteVentasClienteDto
    {
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public int TotalFacturas { get; set; }
        public decimal TotalComprado { get; set; }
    }

    public class EmailRequestDto
    {
        public string Destinatario { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
