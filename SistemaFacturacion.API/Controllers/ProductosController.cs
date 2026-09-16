using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public ProductosController(FacturacionDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return producto;
        }

        [HttpPost]
        public async Task<ActionResult<Producto>> CrearProducto([FromBody] Producto producto)
        {
            if (producto == null || string.IsNullOrWhiteSpace(producto.Nombre))
            {
                return BadRequest("El nombre del producto es obligatorio.");
            }

            producto.Id = 0; // Forzar autoincremento en SQLite
            _db.Productos.Add(producto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarProducto(int id, [FromBody] Producto producto)
        {
            if (id != producto.Id) return BadRequest();
            _db.Entry(producto).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            try
            {
                var detallesFactura = await _db.DetalleFacturas.Where(d => d.ProductoId == id).ToListAsync();
                if (detallesFactura.Any()) _db.DetalleFacturas.RemoveRange(detallesFactura);

                var detallesCotizacion = await _db.DetalleCotizaciones.Where(d => d.ProductoId == id).ToListAsync();
                if (detallesCotizacion.Any()) _db.DetalleCotizaciones.RemoveRange(detallesCotizacion);

                var movimientos = await _db.MovimientosInventario.Where(m => m.ProductoId == id).ToListAsync();
                if (movimientos.Any()) _db.MovimientosInventario.RemoveRange(movimientos);

                _db.Productos.Remove(producto);
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto: {ex.Message}");
            }
        }
    }
}
