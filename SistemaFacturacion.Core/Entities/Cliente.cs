namespace SistemaFacturacion.Core.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public decimal LimiteCredito { get; set; } = 0;
        public decimal SaldoPendiente { get; set; } = 0;
    }
}
