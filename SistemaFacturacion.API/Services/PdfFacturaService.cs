using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaFacturacion.Core.Entities;
using System.Globalization;

namespace SistemaFacturacion.API.Services
{
    public class PdfFacturaService
    {
        public byte[] GenerarPdf(Factura factura)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ── 1. Encabezado ──────────────────────────────────────────────
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SISTEMA DE FACTURACIÓN ELECTRÓNICA").Bold().FontSize(18).FontColor("#4F46E5");
                            col.Item().Text("Soluciones Digitales S.A. de C.V.").FontSize(11).Bold();
                            col.Item().Text("RFC: SFE101010-ABC | Tel: (555) 000-1122");
                            col.Item().Text("Email: facturacion@sistemafactura.com");
                        });

                        row.ConstantItem(180).Column(col =>
                        {
                            col.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Column(box =>
                            {
                                box.Item().Text("FACTURA").Bold().FontSize(14).FontColor("#1E293B").AlignCenter();
                                box.Item().Text($"Folio: {factura.NumeroFactura}").Bold().FontSize(12).FontColor("#EF4444").AlignCenter();
                                box.Item().Text($"Fecha: {factura.Fecha:dd/MM/yyyy HH:mm}").FontSize(9).AlignCenter();
                                box.Item().Text($"Estado: {factura.Estado}").FontSize(9).FontColor("#10B981").Bold().AlignCenter();
                            });
                        });
                    });

                    // ── 2. Contenido Principal ─────────────────────────────────────
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // Datos del Cliente
                        col.Item().Border(1).BorderColor("#E2E8F0").Background("#F1F5F9").Padding(10).Column(clientBox =>
                        {
                            clientBox.Item().Text("DATOS DEL RECEPTOR / CLIENTE").Bold().FontSize(11).FontColor("#334155");
                            clientBox.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                            clientBox.Item().PaddingTop(4).Column(details =>
                            {
                                details.Item().Row(r => { r.RelativeItem().Text($"Nombre: {factura.Cliente?.Nombre ?? "Cliente General"}").Bold(); r.RelativeItem().Text($"RFC/ID: {factura.Cliente?.DocumentoIdentidad ?? "N/A"}"); });
                                details.Item().Row(r => { r.RelativeItem().Text($"Email: {factura.Cliente?.Email ?? "N/A"}"); r.RelativeItem().Text($"Teléfono: {factura.Cliente?.Telefono ?? "N/A"}"); });
                                details.Item().Row(r => { r.RelativeItem().Text($"Dirección: {factura.Cliente?.Direccion ?? "N/A"}"); });
                            });
                        });

                        col.Item().PaddingTop(15);

                        // Tabla de Productos / Detalles
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // #
                                columns.RelativeColumn(3);  // Producto
                                columns.ConstantColumn(60);  // Cantidad
                                columns.ConstantColumn(90);  // P. Unitario
                                columns.ConstantColumn(80);  // IVA
                                columns.ConstantColumn(90);  // Subtotal
                            });

                            // Header de la tabla
                            table.Header(header =>
                            {
                                header.Cell().Background("#4F46E5").Padding(6).Text("#").FontColor(Colors.White).Bold();
                                header.Cell().Background("#4F46E5").Padding(6).Text("Producto / Descripción").FontColor(Colors.White).Bold();
                                header.Cell().Background("#4F46E5").Padding(6).AlignRight().Text("Cant.").FontColor(Colors.White).Bold();
                                header.Cell().Background("#4F46E5").Padding(6).AlignRight().Text("P. Unitario").FontColor(Colors.White).Bold();
                                header.Cell().Background("#4F46E5").Padding(6).AlignRight().Text("Impuesto").FontColor(Colors.White).Bold();
                                header.Cell().Background("#4F46E5").Padding(6).AlignRight().Text("Importe").FontColor(Colors.White).Bold();
                            });

                            int index = 1;
                            foreach (var item in factura.Detalles)
                            {
                                string bgColor = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                                table.Cell().Background(bgColor).Padding(6).Text(index.ToString());
                                table.Cell().Background(bgColor).Padding(6).Text(item.Producto?.Nombre ?? "Producto");
                                table.Cell().Background(bgColor).Padding(6).AlignRight().Text(item.Cantidad.ToString());
                                table.Cell().Background(bgColor).Padding(6).AlignRight().Text(item.PrecioUnitario.ToString("C", CultureInfo.GetCultureInfo("es-MX")));
                                table.Cell().Background(bgColor).Padding(6).AlignRight().Text(item.ImpuestoMonto.ToString("C", CultureInfo.GetCultureInfo("es-MX")));
                                table.Cell().Background(bgColor).Padding(6).AlignRight().Text((item.Subtotal + item.ImpuestoMonto).ToString("C", CultureInfo.GetCultureInfo("es-MX")));

                                index++;
                            }
                        });

                        col.Item().PaddingTop(15);

                        // Totales y Desglose
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Column(leftCol =>
                            {
                                leftCol.Item().Text("Sello Digital / Cadena Original Fiscal:").Bold().FontSize(8);
                                leftCol.Item().Text($"||1.1|FAC-{factura.Id}|{factura.Fecha:yyyy-MM-ddTHH:mm:ss}|{factura.Total}|00001000000504465028||").FontSize(7).FontColor("#64748B");
                                leftCol.Item().PaddingTop(10).Text("Este documento es una representación impresa de un CFDI.").Italic().FontSize(8);
                            });

                            row.RelativeItem(1.5f).Column(rightCol =>
                            {
                                rightCol.Item().Border(1).BorderColor("#CBD5E1").Padding(8).Column(box =>
                                {
                                    box.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.ConstantItem(80).AlignRight().Text(factura.Subtotal.ToString("C", CultureInfo.GetCultureInfo("es-MX"))); });
                                    box.Item().Row(r => { r.RelativeItem().Text("Impuestos (IVA):"); r.ConstantItem(80).AlignRight().Text(factura.ImpuestosTotal.ToString("C", CultureInfo.GetCultureInfo("es-MX"))); });
                                    box.Item().LineHorizontal(1).LineColor("#4F46E5");
                                    box.Item().PaddingTop(4).Row(r => { r.RelativeItem().Text("TOTAL:").Bold().FontSize(12).FontColor("#4F46E5"); r.ConstantItem(100).AlignRight().Text(factura.Total.ToString("C", CultureInfo.GetCultureInfo("es-MX"))).Bold().FontSize(12).FontColor("#4F46E5"); });
                                });
                            });
                        });
                    });

                    // ── 3. Pie de página ───────────────────────────────────────────
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerarTicketPosPdf(Factura factura)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Formato POS 80mm de ancho (~226 pt)
                    page.Size(226, 600);
                    page.Margin(8);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Courier New"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("*** MI TIENDA POS ***").Bold().FontSize(12).AlignCenter();
                        col.Item().Text("NIT: 900.123.456-7").FontSize(8).AlignCenter();
                        col.Item().Text("Tel: (555) 000-1122").FontSize(8).AlignCenter();
                        col.Item().Text("--------------------------------").FontSize(8).AlignCenter();
                        col.Item().Text($"TICKET: {factura.NumeroFactura}").Bold().FontSize(9);
                        col.Item().Text($"FECHA: {factura.Fecha:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"CAJERO: {factura.UsuarioNombre ?? "Cajero"}");
                        col.Item().Text($"CLIENTE: {factura.Cliente?.Nombre ?? "Cliente General"}");
                        col.Item().Text("--------------------------------").FontSize(8).AlignCenter();
                    });

                    page.Content().PaddingVertical(4).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(2).Text("CANT PROD").Bold();
                            r.RelativeItem(1).AlignRight().Text("P.U.").Bold();
                            r.RelativeItem(1).AlignRight().Text("TOT").Bold();
                        });

                        foreach (var item in factura.Detalles)
                        {
                            col.Item().Row(r =>
                            {
                                string nombreProd = item.Producto?.Nombre ?? "Producto";
                                if (nombreProd.Length > 16) nombreProd = nombreProd.Substring(0, 14) + "..";
                                r.RelativeItem(2).Text($"{item.Cantidad}x {nombreProd}");
                                r.RelativeItem(1).AlignRight().Text(item.PrecioUnitario.ToString("N0", CultureInfo.GetCultureInfo("es-CO")));
                                r.RelativeItem(1).AlignRight().Text((item.Subtotal + item.ImpuestoMonto).ToString("N0", CultureInfo.GetCultureInfo("es-CO")));
                            });
                        }

                        col.Item().Text("--------------------------------").FontSize(8).AlignCenter();

                        col.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.RelativeItem().AlignRight().Text(factura.Subtotal.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))); });
                        if (factura.DescuentoMonto > 0)
                            col.Item().Row(r => { r.RelativeItem().Text("Descuento:"); r.RelativeItem().AlignRight().Text($"-{factura.DescuentoMonto.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))}"); });
                        col.Item().Row(r => { r.RelativeItem().Text("IVA:"); r.RelativeItem().AlignRight().Text(factura.ImpuestosTotal.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))); });
                        col.Item().Row(r => { r.RelativeItem().Text("TOTAL:").Bold().FontSize(10); r.RelativeItem().AlignRight().Text(factura.Total.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))).Bold().FontSize(10); });

                        col.Item().Text("--------------------------------").FontSize(8).AlignCenter();
                        col.Item().Row(r => { r.RelativeItem().Text("Forma de Pago:"); r.RelativeItem().AlignRight().Text(factura.FormaPago); });
                        if (factura.PagaCon > 0)
                        {
                            col.Item().Row(r => { r.RelativeItem().Text("Paga Con:"); r.RelativeItem().AlignRight().Text(factura.PagaCon.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))); });
                            col.Item().Row(r => { r.RelativeItem().Text("Cambio:").Bold(); r.RelativeItem().AlignRight().Text(factura.Cambio.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))).Bold(); });
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().Text("--------------------------------").FontSize(8).AlignCenter();
                        col.Item().Text("¡GRACIAS POR SU COMPRA!").Bold().FontSize(9).AlignCenter();
                        col.Item().Text("Conserve este ticket como comprobante").FontSize(7).AlignCenter();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
