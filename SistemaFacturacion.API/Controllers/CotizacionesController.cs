using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CotizacionesController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public CotizacionesController(FacturacionDbContext db)
        {
            _db = db;
        }

        // GET: api/cotizaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cotizacion>>> GetCotizaciones()
        {
            return await _db.Cotizaciones
                .Include(c => c.Cliente)
                .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
        }

        // POST: api/cotizaciones
        [HttpPost]
        public async Task<ActionResult<Cotizacion>> CrearCotizacion([FromBody] CotizacionCreateDto dto)
        {
            if (dto == null || dto.Detalles == null || !dto.Detalles.Any())
            {
                return BadRequest("La cotización debe incluir al menos un producto.");
            }

            var cliente = await _db.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null) return BadRequest("El cliente especificado no existe.");

            var cotizacion = new Cotizacion
            {
                ClienteId = dto.ClienteId,
                NumeroCotizacion = $"COT-2026-{Random.Shared.Next(1000, 9999)}",
                Fecha = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(15),
                Notas = dto.Notas,
                Estado = "Pendiente"
            };

            decimal subtotal = 0;

            foreach (var itemDto in dto.Detalles)
            {
                var prod = await _db.Productos.FindAsync(itemDto.ProductoId);
                if (prod == null) return BadRequest($"El producto #{itemDto.ProductoId} no existe.");

                decimal itemSubtotal = prod.Precio * itemDto.Cantidad;
                subtotal += itemSubtotal;

                cotizacion.Detalles.Add(new DetalleCotizacion
                {
                    ProductoId = prod.Id,
                    Cantidad = itemDto.Cantidad,
                    PrecioUnitario = prod.Precio,
                    Subtotal = itemSubtotal
                });
            }

            decimal impuestos = subtotal * 0.19m;
            cotizacion.Subtotal = subtotal;
            cotizacion.ImpuestosTotal = impuestos;
            cotizacion.Total = subtotal + impuestos;

            _db.Cotizaciones.Add(cotizacion);
            await _db.SaveChangesAsync();

            await _db.Entry(cotizacion).Reference(c => c.Cliente).LoadAsync();

            return Ok(cotizacion);
        }

        // POST: api/cotizaciones/5/convertir-factura
        [HttpPost("{id}/convertir-factura")]
        public async Task<ActionResult<Factura>> ConvertirAFactura(int id)
        {
            var cotizacion = await _db.Cotizaciones
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotizacion == null) return NotFound("Cotización no encontrada.");
            if (cotizacion.Estado == "Convertida") return BadRequest("Esta cotización ya fue convertida a factura.");

            var cliente = await _db.Clientes.FindAsync(cotizacion.ClienteId);
            if (cliente == null) return BadRequest("El cliente especificado no existe.");

            var impuesto = await _db.Impuestos.FirstOrDefaultAsync() ?? new Impuesto { Porcentaje = 19.0m };
            decimal porcentajeIva = impuesto.Porcentaje / 100m;

            var factura = new Factura
            {
                ClienteId = cotizacion.ClienteId,
                UsuarioNombre = "Cajero General",
                Fecha = DateTime.Now,
                NumeroFactura = $"F-2026-{Random.Shared.Next(1000, 9999)}",
                FormaPago = "Efectivo",
                Notas = $"Convertida desde Cotización #{cotizacion.NumeroCotizacion}",
                Estado = "Emitida",
                EstadoPago = "Pagada"
            };

            decimal subtotalBruto = 0;

            foreach (var item in cotizacion.Detalles)
            {
                var producto = await _db.Productos.FindAsync(item.ProductoId);
                if (producto == null) return BadRequest($"Producto #{item.ProductoId} no existe.");

                if (producto.Stock < item.Cantidad)
                {
                    return BadRequest($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, Solicitado: {item.Cantidad}");
                }

                int stockAnterior = producto.Stock;
                producto.Stock -= item.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = "Salida",
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Fecha = DateTime.Now,
                    Motivo = $"Venta Conversión Cotización {cotizacion.NumeroCotizacion}"
                });

                decimal subtotalItem = producto.Precio * item.Cantidad;
                decimal impuestoItem = subtotalItem * porcentajeIva;

                subtotalBruto += subtotalItem;

                factura.Detalles.Add(new DetalleFactura
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = subtotalItem,
                    ImpuestoMonto = impuestoItem
                });
            }

            decimal impuestoTotal = subtotalBruto * porcentajeIva;

            factura.Subtotal = subtotalBruto;
            factura.DescuentoPorcentaje = 0;
            factura.DescuentoMonto = 0;
            factura.ImpuestosTotal = impuestoTotal;
            factura.Total = subtotalBruto + impuestoTotal;

            cotizacion.Estado = "Convertida";

            _db.Facturas.Add(factura);
            await _db.SaveChangesAsync();

            await _db.Entry(factura).Reference(f => f.Cliente).LoadAsync();
            return Ok(factura);
        }
    }
}
