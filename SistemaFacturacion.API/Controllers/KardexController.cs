using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    public class AjusteStockDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public string TipoMovimiento { get; set; } = "Entrada"; // Entrada, Salida, Ajuste
        public string Motivo { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class KardexController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public KardexController(FacturacionDbContext db)
        {
            _db = db;
        }

        // GET: api/kardex
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovimientoInventario>>> GetMovimientos()
        {
            return await _db.MovimientosInventario
                .Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
        }

        // GET: api/kardex/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventario>>> GetKardexProducto(int productoId)
        {
            return await _db.MovimientosInventario
                .Include(m => m.Producto)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
        }

        // POST: api/kardex/ajuste
        [HttpPost("ajuste")]
        public async Task<IActionResult> RegistrarAjuste([FromBody] AjusteStockDto dto)
        {
            var producto = await _db.Productos.FindAsync(dto.ProductoId);
            if (producto == null) return NotFound("Producto no encontrado.");

            int stockAnterior = producto.Stock;
            int stockNuevo = stockAnterior;

            if (dto.TipoMovimiento == "Entrada")
            {
                stockNuevo += dto.Cantidad;
            }
            else if (dto.TipoMovimiento == "Salida")
            {
                if (stockAnterior < dto.Cantidad) return BadRequest("Cantidad a retirar excede el stock actual.");
                stockNuevo -= dto.Cantidad;
            }
            else
            {
                stockNuevo = dto.Cantidad;
            }

            producto.Stock = stockNuevo;

            var movimiento = new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Cantidad = dto.Cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = stockNuevo,
                Fecha = DateTime.Now,
                Motivo = dto.Motivo
            };

            _db.MovimientosInventario.Add(movimiento);
            await _db.SaveChangesAsync();

            return Ok(movimiento);
        }
    }
}
