using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbonosController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public AbonosController(FacturacionDbContext db)
        {
            _db = db;
        }

        // GET: api/abonos/cuentas-por-cobrar
        [HttpGet("cuentas-por-cobrar")]
        public async Task<ActionResult<IEnumerable<CuentaPorCobrarDto>>> GetCuentasPorCobrar()
        {
            var clientes = await _db.Clientes.ToListAsync();
            var result = new List<CuentaPorCobrarDto>();

            foreach (var c in clientes)
            {
                var facturasPendientesCount = await _db.Facturas
                    .CountAsync(f => f.ClienteId == c.Id && f.EstadoPago != "Pagada" && f.Estado != "Anulada");

                if (c.SaldoPendiente > 0 || facturasPendientesCount > 0)
                {
                    result.Add(new CuentaPorCobrarDto
                    {
                        ClienteId = c.Id,
                        ClienteNombre = c.Nombre,
                        DocumentoIdentidad = c.DocumentoIdentidad,
                        Telefono = c.Telefono,
                        LimiteCredito = c.LimiteCredito,
                        SaldoPendiente = c.SaldoPendiente,
                        FacturasPendientesCount = facturasPendientesCount
                    });
                }
            }

            return Ok(result);
        }

        // GET: api/abonos/cliente/5
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<AbonoCliente>>> GetAbonosPorCliente(int clienteId)
        {
            return await _db.AbonosCliente
                .Where(a => a.ClienteId == clienteId)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }

        // POST: api/abonos
        [HttpPost]
        public async Task<ActionResult<AbonoCliente>> RegistrarAbono([FromBody] AbonoCreateDto dto)
        {
            if (dto.Monto <= 0)
            {
                return BadRequest("El monto del abono debe ser mayor a 0.");
            }

            var cliente = await _db.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
            {
                return BadRequest("El cliente especificado no existe.");
            }

            Factura? factura = null;
            if (dto.FacturaId.HasValue && dto.FacturaId.Value > 0)
            {
                factura = await _db.Facturas.FindAsync(dto.FacturaId.Value);
                if (factura != null)
                {
                    factura.SaldoPendiente -= dto.Monto;
                    if (factura.SaldoPendiente <= 0)
                    {
                        factura.SaldoPendiente = 0;
                        factura.EstadoPago = "Pagada";
                    }
                    else
                    {
                        factura.EstadoPago = "Parcial";
                    }
                }
            }

            // Actualizar Saldo Pendiente del Cliente
            cliente.SaldoPendiente -= dto.Monto;
            if (cliente.SaldoPendiente < 0) cliente.SaldoPendiente = 0;

            // Actualizar turno de caja si existe
            if (dto.CajaSesionId.HasValue && dto.CajaSesionId.Value > 0)
            {
                var caja = await _db.CajaSesiones.FindAsync(dto.CajaSesionId.Value);
                if (caja != null && caja.Estado == "Abierta")
                {
                    caja.TotalAbonos += dto.Monto;
                    caja.EfectivoEsperado = caja.MontoInicial + caja.TotalVentasEfectivo + caja.TotalAbonos + caja.TotalIngresos - caja.TotalEgresos;
                }
            }

            var abono = new AbonoCliente
            {
                ClienteId = dto.ClienteId,
                ClienteNombre = cliente.Nombre,
                FacturaId = dto.FacturaId,
                NumeroFactura = factura?.NumeroFactura,
                CajaSesionId = dto.CajaSesionId,
                Monto = dto.Monto,
                Fecha = DateTime.Now,
                FormaPago = string.IsNullOrWhiteSpace(dto.FormaPago) ? "Efectivo" : dto.FormaPago,
                Notas = dto.Notas,
                UsuarioId = dto.UsuarioId,
                UsuarioNombre = dto.UsuarioNombre
            };

            _db.AbonosCliente.Add(abono);
            await _db.SaveChangesAsync();

            return Ok(abono);
        }
    }
}
