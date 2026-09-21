import { Request, Response } from 'express';
import { CreateInvoiceSchema } from '../schemas/billingSchema';
import { InvoiceRepository } from '../repositories/invoiceRepository';
import sanitizeHtml from 'sanitize-html';

export class InvoiceController {
  private invoiceRepo: InvoiceRepository;

  constructor(repo: InvoiceRepository) {
    this.invoiceRepo = repo;
  }

  /**
   * Endpoint Seguro para la Emisión de Facturas
   * POST /api/v1/invoices
   */
  public createInvoice = async (req: Request, res: Response) => {
    try {
      // 1. REQUERIMIENTO 2: Validación Estricta con Zod
      const parseResult = CreateInvoiceSchema.safeParse(req.body);
      if (!parseResult.success) {
        return res.status(400).json({
          error: 'Error de Validación de Datos',
          details: parseResult.error.format(),
        });
      }

      const input = parseResult.data;

      // 2. REQUERIMIENTO 2: Sanitización XSS de Entradas de Texto Libre
      const sanitizedClientName = sanitizeHtml(input.clientName, { allowedTags: [], allowedAttributes: {} });
      const sanitizedTaxId = sanitizeHtml(input.clientTaxId, { allowedTags: [], allowedAttributes: {} });
      const sanitizedEmail = sanitizeHtml(input.clientEmail, { allowedTags: [], allowedAttributes: {} });

      // 3. REQUERIMIENTO 2: Recálculo en Servidor de Impuestos y Totales (Prevención de Manipulación)
      let subtotal = 0;
      let totalTax = 0;

      const processedItems = input.items.map((item) => {
        const cleanProdName = sanitizeHtml(item.productName, { allowedTags: [], allowedAttributes: {} });
        const itemSubtotal = item.quantity * item.unitPrice;
        const itemTax = itemSubtotal * item.taxRate;

        subtotal += itemSubtotal;
        totalTax += itemTax;

        return {
          productName: cleanProdName,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxRate: item.taxRate,
          subtotal: itemSubtotal,
          taxAmount: itemTax,
          total: itemSubtotal + itemTax,
        };
      });

      const grandTotal = subtotal + totalTax;

      // 4. Generar Folio y Persistir via Consultas SQL Parametrizadas
      const invoiceId = `inv_${Date.now()}`;
      const invoiceNumber = `FAC-2026-${Math.floor(1000 + Math.random() * 9000)}`;
      const createdByUserId = req.user?.userId || 'system';

      const savedInvoice = await this.invoiceRepo.createInvoice({
        id: invoiceId,
        invoiceNumber,
        clientName: sanitizedClientName,
        clientTaxId: sanitizedTaxId,
        clientEmail: sanitizedEmail,
        subtotal,
        totalTax,
        grandTotal,
        createdByUserId,
      });

      return res.status(201).json({
        message: 'Factura emitida y registrada exitosamente.',
        data: {
          ...savedInvoice,
          items: processedItems,
        },
      });

    } catch (error) {
      console.error('Error al emitir la factura:', error);
      return res.status(500).json({
        error: 'Error Interno del Servidor',
        message: 'Ocurrió un error al procesar la factura.',
      });
    }
  };
}
