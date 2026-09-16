using System;

namespace SistemaFacturacion.Core.Entities
{
    public class MovimientoInventario
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string TipoMovimiento { get; set; } = "Entrada"; // Entrada, Salida, Ajuste
        public int Cantidad { get; set; }
        public int StockAnterior { get; set; }
        public int StockNuevo { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Motivo { get; set; } = string.Empty;

        public Producto? Producto { get; set; }
    }
}
