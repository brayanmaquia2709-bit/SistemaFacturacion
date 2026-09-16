namespace SistemaFacturacion.Core.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Rol { get; set; } = "Cajero"; // Admin, Cajero, Contador
        public bool Activo { get; set; } = true;
    }
}
