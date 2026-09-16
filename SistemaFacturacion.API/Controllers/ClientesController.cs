using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public ClientesController(FacturacionDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            return await _db.Clientes.OrderBy(c => c.Nombre).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return cliente;
        }

        [HttpPost]
        public async Task<ActionResult<Cliente>> CrearCliente([FromBody] Cliente cliente)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                return BadRequest("El nombre del cliente es obligatorio.");
            }

            cliente.Id = 0; // Forzar autoincremento en SQLite
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] Cliente cliente)
        {
            if (id != cliente.Id) return BadRequest();
            _db.Entry(cliente).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            try
            {
                // Eliminar cotizaciones asociadas si existen para evitar violaciones de clave foránea
                var cotizaciones = await _db.Cotizaciones.Where(c => c.ClienteId == id).ToListAsync();
                if (cotizaciones.Any())
                {
                    _db.Cotizaciones.RemoveRange(cotizaciones);
                }

                // Eliminar facturas asociadas a este cliente (EF Core eliminará en cascada sus detalles)
                var facturas = await _db.Facturas.Where(f => f.ClienteId == id).ToListAsync();
                if (facturas.Any())
                {
                    _db.Facturas.RemoveRange(facturas);
                }

                _db.Clientes.Remove(cliente);
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el cliente: {ex.Message}");
            }
        }
    }
}
