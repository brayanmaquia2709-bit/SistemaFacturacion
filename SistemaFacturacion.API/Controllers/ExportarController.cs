using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.API.Data;

namespace SistemaFacturacion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportarController : ControllerBase
    {
        private readonly FacturacionDbContext _db;

        public ExportarController(FacturacionDbContext db)
        {
            _db = db;
        }

        // GET: api/exportar/ventas-excel
        [HttpGet("ventas-excel")]
        public async Task<IActionResult> ExportarVentasExcel()
        {
            var facturas = await _db.Facturas
                .Include(f => f.Cliente)
                .OrderByDescending(f => f.Fecha)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Reporte de Ventas");

            // Cabeceras
            worksheet.Cell(1, 1).Value = "N° Factura";
            worksheet.Cell(1, 2).Value = "Fecha";
            worksheet.Cell(1, 3).Value = "Cliente";
            worksheet.Cell(1, 4).Value = "Forma de Pago";
            worksheet.Cell(1, 5).Value = "Subtotal";
            worksheet.Cell(1, 6).Value = "Descuento";
            worksheet.Cell(1, 7).Value = "IVA (Impuestos)";
            worksheet.Cell(1, 8).Value = "Total";
            worksheet.Cell(1, 9).Value = "Estado";

            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var f in facturas)
            {
                worksheet.Cell(row, 1).Value = f.NumeroFactura;
                worksheet.Cell(row, 2).Value = f.Fecha.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 3).Value = f.Cliente?.Nombre ?? "Consumidor Final";
                worksheet.Cell(row, 4).Value = f.FormaPago;
                worksheet.Cell(row, 5).Value = f.Subtotal;
                worksheet.Cell(row, 6).Value = f.DescuentoMonto;
                worksheet.Cell(row, 7).Value = f.ImpuestosTotal;
                worksheet.Cell(row, 8).Value = f.Total;
                worksheet.Cell(row, 9).Value = f.Estado;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Ventas_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: api/exportar/productos-excel
        [HttpGet("productos-excel")]
        public async Task<IActionResult> ExportarProductosExcel()
        {
            var productos = await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Catálogo de Productos");

            worksheet.Cell(1, 1).Value = "Código de Barras";
            worksheet.Cell(1, 2).Value = "Nombre Producto";
            worksheet.Cell(1, 3).Value = "Categoría";
            worksheet.Cell(1, 4).Value = "Precio Unitario";
            worksheet.Cell(1, 5).Value = "Stock Disponible";
            worksheet.Cell(1, 6).Value = "Estado Inventario";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var p in productos)
            {
                worksheet.Cell(row, 1).Value = p.CodigoBarra;
                worksheet.Cell(row, 2).Value = p.Nombre;
                worksheet.Cell(row, 3).Value = p.Categoria;
                worksheet.Cell(row, 4).Value = p.Precio;
                worksheet.Cell(row, 5).Value = p.Stock;
                worksheet.Cell(row, 6).Value = p.Stock <= 0 ? "Agotado" : p.Stock <= 5 ? "Stock Bajo" : "Normal";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Inventario_Productos_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
