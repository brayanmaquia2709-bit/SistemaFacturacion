using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImpuestosController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public ImpuestosController(FacturacionDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Impuesto>>> GetImpuestos()
        {
            return await _db.Impuestos.Where(i => i.Activo).ToListAsync();
        }
    }
}
