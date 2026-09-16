namespace SistemaFacturacion.Core.Entities
{
    public class Impuesto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty; // Ej: IVA 16%, Exento, Retención
        public decimal Porcentaje { get; set; }          // Ej: 16.0 para 16%
        public bool Activo { get; set; } = true;
    }
}
