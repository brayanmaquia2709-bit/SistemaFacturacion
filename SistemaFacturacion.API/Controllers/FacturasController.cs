using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.API.Services;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasController : ControllerBase
    {
        private readonly FacturacionDbContext _db;
        private readonly PdfFacturaService _pdfService;
        private readonly CorreoService _correoService;

        public FacturasController(FacturacionDbContext db, PdfFacturaService pdfService, CorreoService correoService)
        {
            _db = db;
            _pdfService = pdfService;
            _correoService = correoService;
        }

        // GET: api/facturas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Factura>>> GetFacturas()
        {
            return await _db.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(f => f.Fecha)
                .ToListAsync();
        }

        // GET: api/facturas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Factura>> GetFactura(int id)
        {
            var factura = await _db.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null)
            {
                return NotFound(new { mensaje = $"No se encontró la factura #{id}" });
            }

            return factura;
        }

        // POST: api/facturas -> Generar nueva factura con descuentos, formas de pago y Kardex
        [HttpPost]
        public async Task<ActionResult<Factura>> GenerarFactura([FromBody] FacturaCreateDto dto)
        {
            if (dto == null || dto.Detalles == null || !dto.Detalles.Any())
            {
                return BadRequest("La factura debe contener al menos un producto.");
            }

            var cliente = await _db.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
            {
                return BadRequest($"El cliente con Id {dto.ClienteId} no existe.");
            }

            var impuesto = await _db.Impuestos.FindAsync(dto.ImpuestoId) ?? await _db.Impuestos.FirstAsync();
            decimal porcentajeIva = impuesto.Porcentaje / 100m;

            string? nombreUsuario = null;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                var usuarioDb = await _db.Usuarios.FindAsync(dto.UsuarioId.Value);
                if (usuarioDb != null)
                {
                    nombreUsuario = usuarioDb.Nombre;
                }
            }

            var factura = new Factura
            {
                ClienteId = dto.ClienteId,
                UsuarioId = dto.UsuarioId,
                UsuarioNombre = nombreUsuario ?? "Cajero General",
                Fecha = DateTime.Now,
                NumeroFactura = $"F-2026-{Random.Shared.Next(1000, 9999)}",
                FormaPago = string.IsNullOrWhiteSpace(dto.FormaPago) ? "Efectivo" : dto.FormaPago,
                DescuentoPorcentaje = dto.DescuentoPorcentaje,
                Notas = dto.Notas,
                Estado = "Emitida",
                EstadoPago = dto.FormaPago == "Credito" ? "Pendiente" : "Pagada"
            };

            decimal subtotalBruto = 0;

            foreach (var itemDto in dto.Detalles)
            {
                var producto = await _db.Productos.FindAsync(itemDto.ProductoId);
                if (producto == null)
                {
                    return BadRequest($"El producto con Id {itemDto.ProductoId} no existe.");
                }

                if (producto.Stock < itemDto.Cantidad)
                {
                    return BadRequest($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, Solicitado: {itemDto.Cantidad}");
                }

                int stockAnterior = producto.Stock;
                producto.Stock -= itemDto.Cantidad;

                // Registrar Kardex
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = "Salida",
                    Cantidad = itemDto.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Fecha = DateTime.Now,
                    Motivo = $"Venta Factura {factura.NumeroFactura}"
                });

                decimal subtotalItem = producto.Precio * itemDto.Cantidad;
                decimal impuestoItem = subtotalItem * porcentajeIva;

                subtotalBruto += subtotalItem;

                factura.Detalles.Add(new DetalleFactura
                {
                    ProductoId = producto.Id,
                    Cantidad = itemDto.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = subtotalItem,
                    ImpuestoMonto = impuestoItem
                });
            }

            decimal descuentoMonto = subtotalBruto * (dto.DescuentoPorcentaje / 100m);
            decimal subtotalNeto = subtotalBruto - descuentoMonto;
            decimal impuestoTotal = subtotalNeto * porcentajeIva;
            decimal totalFactura = subtotalNeto + impuestoTotal;

            // Validación de Crédito
            if (dto.FormaPago == "Credito")
            {
                if (cliente.LimiteCredito > 0 && (cliente.SaldoPendiente + totalFactura) > cliente.LimiteCredito)
                {
                    return BadRequest($"El total de la factura ({totalFactura:C}) excede el límite de crédito permitido para este cliente ({cliente.LimiteCredito:C}). Saldo actual: {cliente.SaldoPendiente:C}");
                }
                cliente.SaldoPendiente += totalFactura;
                factura.SaldoPendiente = totalFactura;
                factura.EstadoPago = "Pendiente";
            }
            else
            {
                factura.PagaCon = dto.PagaCon;
                factura.Cambio = dto.Cambio > 0 ? dto.Cambio : (dto.PagaCon > 0 && dto.PagaCon > totalFactura ? dto.PagaCon - totalFactura : 0);
                factura.SaldoPendiente = 0;
                factura.EstadoPago = "Pagada";
            }

            factura.Subtotal = subtotalBruto;
            factura.DescuentoMonto = descuentoMonto;
            factura.ImpuestosTotal = impuestoTotal;
            factura.Total = totalFactura;
            factura.CajaSesionId = dto.CajaSesionId;

            // Actualizar Caja si hay turno activo o si se envió CajaSesionId
            CajaSesion? cajaActiva = null;
            if (dto.CajaSesionId.HasValue && dto.CajaSesionId.Value > 0)
            {
                cajaActiva = await _db.CajaSesiones.FindAsync(dto.CajaSesionId.Value);
            }
            else if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                cajaActiva = await _db.CajaSesiones.FirstOrDefaultAsync(c => c.UsuarioId == dto.UsuarioId.Value && c.Estado == "Abierta");
            }

            if (cajaActiva != null && cajaActiva.Estado == "Abierta")
            {
                factura.CajaSesionId = cajaActiva.Id;
                if (dto.FormaPago == "Efectivo") cajaActiva.TotalVentasEfectivo += totalFactura;
                else if (dto.FormaPago == "Tarjeta" || dto.FormaPago == "TarjetaCredito" || dto.FormaPago == "TarjetaDebito") cajaActiva.TotalVentasTarjeta += totalFactura;
                else if (dto.FormaPago == "Transferencia") cajaActiva.TotalVentasTransferencia += totalFactura;
                else if (dto.FormaPago == "Credito") cajaActiva.TotalVentasCredito += totalFactura;

                cajaActiva.EfectivoEsperado = cajaActiva.MontoInicial + cajaActiva.TotalVentasEfectivo + cajaActiva.TotalAbonos + cajaActiva.TotalIngresos - cajaActiva.TotalEgresos;
            }

            _db.Facturas.Add(factura);
            await _db.SaveChangesAsync();

            await _db.Entry(factura).Reference(f => f.Cliente).LoadAsync();
            foreach (var d in factura.Detalles)
            {
                await _db.Entry(d).Reference(det => det.Producto).LoadAsync();
            }

            if (!string.IsNullOrEmpty(cliente.Email))
            {
                _ = Task.Run(async () =>
                {
                    var pdf = _pdfService.GenerarPdf(factura);
                    await _correoService.SendInvoiceEmailAsync(cliente.Email, factura.NumeroFactura, pdf);
                });
            }

            return CreatedAtAction(nameof(GetFactura), new { id = factura.Id }, factura);
        }

        // POST: api/facturas/5/anular -> Emite Nota de Crédito y devuelve stock
        [HttpPost("{id}/anular")]
        public async Task<IActionResult> AnularFactura(int id, [FromBody] string? motivo)
        {
            var factura = await _db.Facturas
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound("Factura no encontrada.");
            if (factura.Estado == "Anulada") return BadRequest("La factura ya se encuentra anulada.");

            factura.Estado = "Anulada";
            string motivoAnulacion = string.IsNullOrWhiteSpace(motivo) ? "Anulación por solicitud del cliente" : motivo;

            // Reintegrar stock y registrar Kardex
            foreach (var det in factura.Detalles)
            {
                if (det.Producto != null)
                {
                    int stockAnterior = det.Producto.Stock;
                    det.Producto.Stock += det.Cantidad;

                    _db.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = det.Producto.Id,
                        TipoMovimiento = "Entrada",
                        Cantidad = det.Cantidad,
                        StockAnterior = stockAnterior,
                        StockNuevo = det.Producto.Stock,
                        Fecha = DateTime.Now,
                        Motivo = $"Anulación Factura #{factura.NumeroFactura}"
                    });
                }
            }

            // Crear Nota de Crédito
            var notaCredito = new NotaCredito
            {
                FacturaId = factura.Id,
                NumeroNota = $"NC-2026-{Random.Shared.Next(1000, 9999)}",
                Fecha = DateTime.Now,
                Motivo = motivoAnulacion,
                MontoTotal = factura.Total
            };

            _db.NotasCredito.Add(notaCredito);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Factura #{factura.NumeroFactura} anulada correctamente. Nota de Crédito #{notaCredito.NumeroNota} generada." });
        }

        // GET: api/facturas/5/pdf -> Descargar PDF
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var factura = await _db.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound("Factura no encontrada.");

            byte[] pdfBytes = _pdfService.GenerarPdf(factura);
            return File(pdfBytes, "application/pdf", $"Factura_{factura.NumeroFactura}.pdf");
        }

        // GET: api/facturas/5/ticket -> Descargar Ticket Térmico POS 80mm
        [HttpGet("{id}/ticket")]
        public async Task<IActionResult> DescargarTicketPos(int id)
        {
            var factura = await _db.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound("Factura no encontrada.");

            byte[] pdfBytes = _pdfService.GenerarTicketPosPdf(factura);
            return File(pdfBytes, "application/pdf", $"Ticket_{factura.NumeroFactura}.pdf");
        }

        // POST: api/facturas/5/correo -> Enviar por correo
        [HttpPost("{id}/correo")]
        public async Task<IActionResult> EnviarCorreo(int id, [FromBody] EmailRequestDto? dto)
        {
            var factura = await _db.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound("Factura no encontrada.");

            string emailDestino = dto?.Destinatario ?? factura.Cliente?.Email ?? string.Empty;
            if (string.IsNullOrEmpty(emailDestino))
            {
                return BadRequest("No se especificó un correo electrónico de destino.");
            }

            byte[] pdfBytes = _pdfService.GenerarPdf(factura);
            await _correoService.SendInvoiceEmailAsync(emailDestino, factura.NumeroFactura, pdfBytes);

            return Ok(new { mensaje = $"Factura #{factura.NumeroFactura} enviada correctamente a {emailDestino}" });
        }
    }
}
