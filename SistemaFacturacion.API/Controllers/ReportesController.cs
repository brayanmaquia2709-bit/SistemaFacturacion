using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.DTOs;
using System.Globalization;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public ReportesController(FacturacionDbContext db) => _db = db;

        // GET: api/reportes/ventas-mes -> Ventas por mes
        [HttpGet("ventas-mes")]
        public async Task<ActionResult<IEnumerable<ReporteVentasMesDto>>> GetVentasPorMes()
        {
            var facturas = await _db.Facturas.ToListAsync();

            var resultado = facturas
                .GroupBy(f => new { f.Fecha.Year, f.Fecha.Month })
                .Select(g => new ReporteVentasMesDto
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    NombreMes = CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(g.Key.Month),
                    TotalFacturas = g.Count(),
                    TotalVentas = g.Sum(f => f.Total)
                })
                .OrderBy(r => r.Anio)
                .ThenBy(r => r.Mes)
                .ToList();

            return Ok(resultado);
        }

        // GET: api/reportes/ventas-cliente -> Ventas por cliente
        [HttpGet("ventas-cliente")]
        public async Task<ActionResult<IEnumerable<ReporteVentasClienteDto>>> GetVentasPorCliente()
        {
            var facturas = await _db.Facturas.Include(f => f.Cliente).ToListAsync();

            var resultado = facturas
                .GroupBy(f => new { f.ClienteId, Nombre = f.Cliente?.Nombre ?? "Cliente General" })
                .Select(g => new ReporteVentasClienteDto
                {
                    ClienteId = g.Key.ClienteId,
                    NombreCliente = g.Key.Nombre,
                    TotalFacturas = g.Count(),
                    TotalComprado = g.Sum(f => f.Total)
                })
                .OrderByDescending(r => r.TotalComprado)
                .ToList();

            return Ok(resultado);
        }

        // GET: api/reportes/ventas-usuario -> Ventas por usuario / vendedor
        [HttpGet("ventas-usuario")]
        public async Task<ActionResult<IEnumerable<VentasPorUsuarioDto>>> GetVentasPorUsuario()
        {
            var usuarios = await _db.Usuarios.ToListAsync();
            var facturas = await _db.Facturas.Where(f => f.Estado != "Anulada").ToListAsync();

            var resultado = new List<VentasPorUsuarioDto>();

            foreach (var u in usuarios)
            {
                var ventasUser = facturas.Where(f => f.UsuarioId == u.Id || f.UsuarioNombre == u.Nombre).ToList();
                resultado.Add(new VentasPorUsuarioDto
                {
                    UsuarioId = u.Id,
                    NombreUsuario = u.Nombre,
                    Rol = u.Rol,
                    CantidadVentas = ventasUser.Count,
                    TotalVendido = ventasUser.Sum(f => f.Total)
                });
            }

            // También incluir ventas sin usuario específico (Cajero General)
            var ventasSinUser = facturas.Where(f => !f.UsuarioId.HasValue && (string.IsNullOrEmpty(f.UsuarioNombre) || f.UsuarioNombre == "Cajero General")).ToList();
            if (ventasSinUser.Any())
            {
                resultado.Add(new VentasPorUsuarioDto
                {
                    UsuarioId = 0,
                    NombreUsuario = "Cajero General (POS Web / POS App)",
                    Rol = "Cajero",
                    CantidadVentas = ventasSinUser.Count,
                    TotalVendido = ventasSinUser.Sum(f => f.Total)
                });
            }

            return Ok(resultado.OrderByDescending(r => r.TotalVendido).ToList());
        }
    }
}
