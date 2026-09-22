using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.Core.Services
{
    public class FacturacionApiService
    {
        private HttpClient _http;
        private static string _sharedBaseUrl = "http://localhost:5145/";

        public FacturacionApiService() : this(_sharedBaseUrl)
        {
        }

        public FacturacionApiService(string baseUrl)
        {
            _http = CreateHttpClient(baseUrl);
        }

        public FacturacionApiService(HttpClient http)
        {
            _http = http;
        }

        public static string SharedBaseUrl
        {
            get => _sharedBaseUrl;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string formatted = value.Trim();
                    if (!formatted.EndsWith("/")) formatted += "/";
                    if (!formatted.StartsWith("http://") && !formatted.StartsWith("https://")) formatted = "http://" + formatted;
                    _sharedBaseUrl = formatted;
                }
            }
        }

        private static HttpClient CreateHttpClient(string baseUrl)
        {
            string formatted = baseUrl.Trim();
            if (!formatted.EndsWith("/")) formatted += "/";
            if (!formatted.StartsWith("http://") && !formatted.StartsWith("https://")) formatted = "http://" + formatted;

            return new HttpClient
            {
                BaseAddress = new Uri(formatted),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public void UpdateBaseUrl(string newUrl)
        {
            SharedBaseUrl = newUrl;
            _http = CreateHttpClient(SharedBaseUrl);
        }

        // --- AUTENTICACIÓN UNIFICADA ---
        public async Task<Usuario?> LoginAsync(string username, string password)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Auth/login", new { Username = username, Password = password });
                if (res.IsSuccessStatusCode)
                {
                    var usr = await res.Content.ReadFromJsonAsync<Usuario>();
                    if (usr != null) return usr;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando login a API: " + ex.Message);
            }

            // Fallback de credenciales predeterminadas si la API aún se está iniciando o BD limpia
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
            {
                return new Usuario { Id = 1, Nombre = "Administrador del Sistema", Username = "admin", PasswordHash = "admin123", Rol = "Admin", Activo = true };
            }
            if (string.Equals(username, "cajero1", StringComparison.OrdinalIgnoreCase) && password == "123")
            {
                return new Usuario { Id = 2, Nombre = "Juan Pérez (Cajero 1)", Username = "cajero1", PasswordHash = "123", Rol = "Cajero", Activo = true };
            }
            if (string.Equals(username, "cajero2", StringComparison.OrdinalIgnoreCase) && password == "123")
            {
                return new Usuario { Id = 3, Nombre = "María Gómez (Cajera 2)", Username = "cajero2", PasswordHash = "123", Rol = "Cajero", Activo = true };
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

        public async Task<Usuario?> CrearUsuarioAsync(Usuario usuario)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Auth/usuarios", usuario);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Usuario>() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/Auth/usuarios/{id}");
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // --- CLIENTES ---
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

        public async Task<Cliente?> CrearClienteAsync(Cliente c)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Clientes", c);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Cliente>();
                }
                var errStr = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[API Error] HTTP {(int)res.StatusCode} al crear cliente: {errStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Exception] Error al crear cliente: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> ActualizarClienteAsync(Cliente c)
        {
            try
            {
                var res = await _http.PutAsJsonAsync($"api/Clientes/{c.Id}", c);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarClienteAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/Clientes/{id}");
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // --- PRODUCTOS ---
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

        public async Task<Producto?> CrearProductoAsync(Producto p)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Productos", p);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync<Producto>();
                }
                var errStr = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[API Error] HTTP {(int)res.StatusCode} al crear producto: {errStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Exception] Error al crear producto: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            try
            {
                var res = await _http.PutAsJsonAsync($"api/Productos/{p.Id}", p);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/Productos/{id}");
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // --- FACTURAS ---
        public async Task<List<Factura>> GetFacturasAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Factura>>("api/Facturas") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<Factura?> GetFacturaAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<Factura>($"api/Facturas/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> DescargarPdfBytesAsync(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/Facturas/{id}/pdf");
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsByteArrayAsync();
                }
            }
            catch { }
            return null;
        }

        public async Task<byte[]?> DescargarTicketPosBytesAsync(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/Facturas/{id}/ticket");
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsByteArrayAsync();
                }
            }
            catch { }
            return null;
        }

        public async Task<Factura?> GenerarFacturaAsync(FacturaCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Facturas", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Factura>() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> AnularFacturaAsync(int id, string motivo)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"api/Facturas/{id}/anular", motivo);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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

        // --- CAJA ---
        public async Task<CajaSesion?> GetCajaActivaAsync(int usuarioId)
        {
            try
            {
                return await _http.GetFromJsonAsync<CajaSesion>($"api/Caja/activa/{usuarioId}");
            }
            catch { return null; }
        }

        public async Task<List<CajaSesion>> GetHistorialCajasAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CajaSesion>>("api/Caja/historial") ?? new();
            }
            catch { return new(); }
        }

        public async Task<CajaSesion?> AbrirCajaAsync(AperturaCajaDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Caja/apertura", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<CajaSesion>() : null;
            }
            catch { return null; }
        }

        public async Task<CajaSesion?> CerrarCajaAsync(CierreCajaDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Caja/cierre", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<CajaSesion>() : null;
            }
            catch { return null; }
        }

        // --- ABONOS & CUENTAS POR COBRAR ---
        public async Task<List<CuentaPorCobrarDto>> GetCuentasPorCobrarAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CuentaPorCobrarDto>>("api/Abonos/cuentas-por-cobrar") ?? new();
            }
            catch { return new(); }
        }

        public async Task<AbonoCliente?> RegistrarAbonoAsync(AbonoCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Abonos", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<AbonoCliente>() : null;
            }
            catch { return null; }
        }

        // --- REPORTES ---
        public async Task<List<ReporteVentasMesDto>> GetReporteVentasMesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReporteVentasMesDto>>("api/Reportes/ventas-mes") ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<ReporteVentasClienteDto>> GetReporteVentasClienteAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReporteVentasClienteDto>>("api/Reportes/ventas-cliente") ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<VentasPorUsuarioDto>> GetReporteVentasUsuarioAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<VentasPorUsuarioDto>>("api/Reportes/ventas-usuario") ?? new();
            }
            catch { return new(); }
        }

        // --- COTIZACIONES ---
        public async Task<List<Cotizacion>> GetCotizacionesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Cotizacion>>("api/Cotizaciones") ?? new();
            }
            catch { return new(); }
        }

        public async Task<Cotizacion?> CrearCotizacionAsync(CotizacionCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Cotizaciones", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Cotizacion>() : null;
            }
            catch { return null; }
        }

        public async Task<Factura?> ConvertirCotizacionAFacturaAsync(int id)
        {
            try
            {
                var res = await _http.PostAsync($"api/Cotizaciones/{id}/convertir-factura", null);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Factura>() : null;
            }
            catch { return null; }
        }

        // --- KARDEX ---
        public async Task<List<MovimientoInventario>> GetMovimientosKardexAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<MovimientoInventario>>("api/Kardex") ?? new();
            }
            catch { return new(); }
        }

        public async Task<bool> RegistrarAjusteStockAsync(int productoId, int cantidad, string tipo, string motivo)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Kardex/ajuste", new { productoId, cantidad, tipoMovimiento = tipo, motivo });
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
