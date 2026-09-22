using SistemaFacturacion.Core.Services;

namespace SistemaFacturacion.Blazor.Services
{
    // Servicio Web heredado directamente de la capa unificada SistemaFacturacion.Core
    public class FacturacionApiService : SistemaFacturacion.Core.Services.FacturacionApiService
    {
        public FacturacionApiService(HttpClient http) : base(http)
        {
        }
    }
}
