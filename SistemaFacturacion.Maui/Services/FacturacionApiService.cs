using Microsoft.Maui.Controls;
using SistemaFacturacion.Core.Services;

namespace SistemaFacturacion.Maui.Services
{
    // Servicio MAUI heredado directamente de la capa unificada SistemaFacturacion.Core
    public class FacturacionApiService : SistemaFacturacion.Core.Services.FacturacionApiService
    {
        public FacturacionApiService() : base(GetConfiguredBaseUrl())
        {
        }

        public static string GetConfiguredBaseUrl()
        {
            string defaultUrl = "http://localhost:5145/";
            string savedUrl = Preferences.Get("ServerApiUrl", defaultUrl);
            if (!savedUrl.EndsWith("/")) savedUrl += "/";
            if (!savedUrl.StartsWith("http://") && !savedUrl.StartsWith("https://")) savedUrl = "http://" + savedUrl;
            return savedUrl;
        }

        public static void SetConfiguredBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            string formattedUrl = url.Trim();
            if (!formattedUrl.EndsWith("/")) formattedUrl += "/";
            if (!formattedUrl.StartsWith("http://") && !formattedUrl.StartsWith("https://")) formattedUrl = "http://" + formattedUrl;
            Preferences.Set("ServerApiUrl", formattedUrl);
            SharedBaseUrl = formattedUrl;
        }
    }
}
