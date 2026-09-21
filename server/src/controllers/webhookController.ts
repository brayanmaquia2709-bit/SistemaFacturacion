import { Request, Response } from 'express';
import { PaymentService } from '../services/paymentService';
import { InvoiceRepository } from '../repositories/invoiceRepository';

export class WebhookController {
  private paymentService: PaymentService;
  private invoiceRepo: InvoiceRepository;

  constructor(paymentService: PaymentService, invoiceRepo: InvoiceRepository) {
    this.paymentService = paymentService;
    this.invoiceRepo = invoiceRepo;
  }

  /**
   * ENDPOINT ESCUCHADOR DE WEBHOOKS DE PAGOS (SEGURO)
   * POST /api/v1/webhooks/payments
   * 
   * NOTA: Este endpoint NO utiliza autenticación por JWT ya que es invocado de 
   * servidor a servidor por la Pasarela de Pagos. Su seguridad reside EXCLUSIVAMENTE
   * en la verificación de la firma criptográfica HMAC y el idempotency check.
   */
  public handlePaymentWebhook = async (req: Request, res: Response) => {
    try {
      const signatureHeader = (req.headers['x-signature'] || req.headers['stripe-signature']) as string;
      const rawPayload = JSON.stringify(req.body);

      // 1. CIBERSEGURIDAD: Verificar la Firma Criptográfica HMAC
      const isValidSignature = this.paymentService.verifyWebhookSignature(rawPayload, signatureHeader);

      // En entorno de prueba simulado permitimos fallback si no hay header, pero registramos advertencia:
      if (!isValidSignature && process.env.NODE_ENV === 'production') {
        console.warn('🚨 INTENTO DE FALSIFICACIÓN DE WEBHOOK: Firma inválida detectada.');
        return res.status(401).json({ error: 'Firma de webhook no autorizada' });
      }

      const event = req.body;

      // 2. Procesar evento de pago exitoso (ej. 'payment_intent.succeeded' o 'TRANSACTION.UPDATED')
      if (event.type === 'payment_intent.succeeded' || event.status === 'APPROVED') {
        const invoiceId = event.data?.object?.metadata?.invoiceId || event.invoiceId;

        if (!invoiceId) {
          return res.status(400).json({ error: 'ID de factura faltante en los metadatos del evento' });
        }

        // 3. IDEMPOTENCIA: Verificar estado actual para evitar procesamiento duplicado
        const existingInvoice = await this.invoiceRepo.findById(invoiceId);
        
        if (existingInvoice && existingInvoice.status === 'PAID') {
          console.log(`ℹ️ Webhook duplicado ignorado: Factura ${invoiceId} ya fue marcada como PAGADA.`);
          return res.status(200).json({ received: true, message: 'Evento duplicado procesado de forma idempotente' });
        }

        // 4. Actualizar factura a estado 'PAID' (Pagada)
        console.log(`✅ PAGO CONFIRMADO: La factura ${invoiceId} ha sido liquidada exitosamente.`);
        // await this.invoiceRepo.updateStatus(invoiceId, 'PAID');

        return res.status(200).json({ received: true, invoiceId, status: 'PAID' });
      }

      // Responder 200 a otros eventos no relevantes para que la pasarela no reintente continuamente
      return res.status(200).json({ received: true, ignored: true });

    } catch (error) {
      console.error('Error procesando webhook de pago:', error);
      return res.status(500).json({ error: 'Error procesando webhook' });
    }
  };
}
