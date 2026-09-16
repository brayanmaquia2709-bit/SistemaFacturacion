using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public CajaController(FacturacionDbContext db)
        {
            _db = db;
        }

        // GET: api/caja/activa/1
        [HttpGet("activa/{usuarioId}")]
        public async Task<ActionResult<CajaSesion>> GetCajaActiva(int usuarioId)
        {
            var caja = await _db.CajaSesiones
                .Include(c => c.Movimientos)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Estado == "Abierta");

            if (caja == null)
            {
                return NotFound(new { mensaje = "No hay un turno de caja abierto para este usuario." });
            }

            return Ok(caja);
        }

        // POST: api/caja/apertura
        [HttpPost("apertura")]
        public async Task<ActionResult<CajaSesion>> AbrirCaja([FromBody] AperturaCajaDto dto)
        {
            var existeAbierta = await _db.CajaSesiones
                .AnyAsync(c => c.UsuarioId == dto.UsuarioId && c.Estado == "Abierta");

            if (existeAbierta)
            {
                return BadRequest("Ya existe una caja abierta para este usuario.");
            }

            var caja = new CajaSesion
            {
                UsuarioId = dto.UsuarioId,
                UsuarioNombre = string.IsNullOrWhiteSpace(dto.UsuarioNombre) ? "Cajero" : dto.UsuarioNombre,
                FechaApertura = DateTime.Now,
                MontoInicial = dto.MontoInicial,
                EfectivoEsperado = dto.MontoInicial,
                Estado = "Abierta"
            };

            _db.CajaSesiones.Add(caja);
            await _db.SaveChangesAsync();

            return Ok(caja);
        }

        // POST: api/caja/movimiento
        [HttpPost("movimiento")]
        public async Task<ActionResult> RegistrarMovimiento([FromBody] MovimientoCajaDto dto)
        {
            var caja = await _db.CajaSesiones.FindAsync(dto.CajaSesionId);
            if (caja == null || caja.Estado != "Abierta")
            {
                return BadRequest("No existe una caja abierta válida para registrar el movimiento.");
            }

            var mov = new MovimientoCaja
            {
                CajaSesionId = dto.CajaSesionId,
                Tipo = dto.Tipo,
                Monto = dto.Monto,
                Concepto = dto.Concepto,
                Fecha = DateTime.Now,
                UsuarioId = dto.UsuarioId,
                UsuarioNombre = dto.UsuarioNombre
            };

            _db.MovimientosCaja.Add(mov);

            if (dto.Tipo == "Ingreso")
            {
                caja.TotalIngresos += dto.Monto;
            }
            else
            {
                caja.TotalEgresos += dto.Monto;
            }

            caja.EfectivoEsperado = caja.MontoInicial + caja.TotalVentasEfectivo + caja.TotalAbonos + caja.TotalIngresos - caja.TotalEgresos;

            await _db.SaveChangesAsync();
            return Ok(caja);
        }

        // POST: api/caja/cierre
        [HttpPost("cierre")]
        public async Task<ActionResult<CajaSesion>> CerrarCaja([FromBody] CierreCajaDto dto)
        {
            var caja = await _db.CajaSesiones
                .Include(c => c.Movimientos)
                .FirstOrDefaultAsync(c => c.Id == dto.CajaSesionId);

            if (caja == null || caja.Estado != "Abierta")
            {
                return BadRequest("La caja especificada no se encuentra abierta.");
            }

            caja.FechaCierre = DateTime.Now;
            caja.EfectivoEsperado = caja.MontoInicial + caja.TotalVentasEfectivo + caja.TotalAbonos + caja.TotalIngresos - caja.TotalEgresos;
            caja.EfectivoReal = dto.EfectivoReal;
            caja.Diferencia = dto.EfectivoReal - caja.EfectivoEsperado;
            caja.NotasCierre = dto.NotasCierre;
            caja.Estado = "Cerrada";

            await _db.SaveChangesAsync();
            return Ok(caja);
        }

        // GET: api/caja/historial
        [HttpGet("historial")]
        public async Task<ActionResult<IEnumerable<CajaSesion>>> GetHistorialCajas()
        {
            return await _db.CajaSesiones
                .OrderByDescending(c => c.FechaApertura)
                .Take(50)
                .ToListAsync();
        }
    }
}
