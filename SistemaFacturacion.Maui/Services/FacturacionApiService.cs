using System.Net.Http.Json;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.Maui.Services
{
    public class FacturacionApiService
    {
        private HttpClient _http;

        public FacturacionApiService()
        {
            string baseUrl = GetConfiguredBaseUrl();
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public static string GetConfiguredBaseUrl()
        {
            string defaultUrl = "https://sistemafacturacion-oh2n.onrender.com/";
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
        }

        public void UpdateBaseUrl(string newUrl)
        {
            SetConfiguredBaseUrl(newUrl);
            string formattedUrl = GetConfiguredBaseUrl();
            _http = new HttpClient
            {
                BaseAddress = new Uri(formattedUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<List<Cliente>> GetClientesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Cliente>>("api/Clientes") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<Cliente?> CrearClienteAsync(Cliente cliente)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Clientes", cliente);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Cliente>();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<List<Producto>> GetProductosAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Producto>>("api/Productos") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<Producto?> CrearProductoAsync(Producto producto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Productos", producto);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Producto>();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<Factura?> GenerarFacturaAsync(FacturaCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Facturas", dto);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Factura>();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<bool> EnviarCorreoAsync(int facturaId, string email)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"api/Facturas/{facturaId}/correo", new EmailRequestDto { Destinatario = email });
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]?> DescargarPdfBytesAsync(int facturaId)
        {
            try
            {
                var res = await _http.GetAsync($"api/Facturas/{facturaId}/pdf");
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsByteArrayAsync();
                }
            }
            catch
            {
            }
            return null;
        }
        public async Task<Usuario?> LoginAsync(string username, string password)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Auth/login", new LoginRequestDto { Username = username, Password = password });
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Usuario>();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Usuario>>("api/Auth/usuarios") ?? new();
            }
            catch
            {
                return new();
            }
        }

        // --- CAJA ---
        public async Task<CajaSesion?> GetCajaActivaAsync(int usuarioId)
        {
            try
            {
                return await _http.GetFromJsonAsync<CajaSesion>($"api/Caja/activa/{usuarioId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<CajaSesion?> AbrirCajaAsync(AperturaCajaDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Caja/apertura", dto);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<CajaSesion>();
            }
            catch { }
            return null;
        }

        public async Task<bool> RegistrarMovimientoCajaAsync(MovimientoCajaDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Caja/movimiento", dto);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<CajaSesion?> CerrarCajaAsync(CierreCajaDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Caja/cierre", dto);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<CajaSesion>();
            }
            catch { }
            return null;
        }

        // --- ABONOS Y CUENTAS POR COBRAR ---
        public async Task<List<CuentaPorCobrarDto>> GetCuentasPorCobrarAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CuentaPorCobrarDto>>("api/Abonos/cuentas-por-cobrar") ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<AbonoCliente>> GetAbonosPorClienteAsync(int clienteId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AbonoCliente>>($"api/Abonos/cliente/{clienteId}") ?? new();
            }
            catch { return new(); }
        }

        public async Task<AbonoCliente?> RegistrarAbonoAsync(AbonoCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Abonos", dto);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<AbonoCliente>();
            }
            catch { }
            return null;
        }

        // --- TICKET POS ---
        public async Task<byte[]?> DescargarTicketPosBytesAsync(int facturaId)
        {
            try
            {
                var res = await _http.GetAsync($"api/Facturas/{facturaId}/ticket");
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadAsByteArrayAsync();
            }
            catch { }
            return null;
        }

        // --- ELIMINACIONES ---
        public async Task<bool> EliminarClienteAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/Clientes/{id}");
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/Productos/{id}");
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
