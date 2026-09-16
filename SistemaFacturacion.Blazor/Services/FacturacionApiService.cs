using System.Net.Http.Json;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.Blazor.Services
{
    public class FacturacionApiService
    {
        private readonly HttpClient _http;

        public FacturacionApiService(HttpClient http)
        {
            _http = http;
        }

        // Clientes
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
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Cliente>() : null;
            }
            catch
            {
                return null;
            }
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

        // Productos
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
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Producto>() : null;
            }
            catch
            {
                return null;
            }
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

        // Facturas
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

        // Cotizaciones
        public async Task<List<Cotizacion>> GetCotizacionesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Cotizacion>>("api/Cotizaciones") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<Cotizacion?> CrearCotizacionAsync(CotizacionCreateDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Cotizaciones", dto);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Cotizacion>() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Factura?> ConvertirCotizacionAFacturaAsync(int id)
        {
            try
            {
                var res = await _http.PostAsync($"api/Cotizaciones/{id}/convertir-factura", null);
                return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<Factura>() : null;
            }
            catch
            {
                return null;
            }
        }

        // Kardex
        public async Task<List<MovimientoInventario>> GetMovimientosKardexAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<MovimientoInventario>>("api/Kardex") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<bool> RegistrarAjusteStockAsync(int productoId, int cantidad, string tipo, string motivo)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/Kardex/ajuste", new { productoId, cantidad, tipoMovimiento = tipo, motivo });
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Reportes
        public async Task<List<ReporteVentasMesDto>> GetReporteVentasMesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReporteVentasMesDto>>("api/Reportes/ventas-mes") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<ReporteVentasClienteDto>> GetReporteVentasClienteAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReporteVentasClienteDto>>("api/Reportes/ventas-cliente") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<VentasPorUsuarioDto>> GetReporteVentasUsuarioAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<VentasPorUsuarioDto>>("api/Reportes/ventas-usuario") ?? new();
            }
            catch
            {
                return new();
            }
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

        // Caja
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

        // Abonos & Cuentas por Cobrar
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
    }
}
