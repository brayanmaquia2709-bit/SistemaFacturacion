import { z } from 'zod';

/**
 * REQUERIMIENTO 2: Validación Estricta de Datos (Zod)
 * Previene manipulación de campos, correos maliciosos y montos negativos.
 */

export const CreateInvoiceItemSchema = z.object({
  productName: z.string().min(1, 'El nombre del producto es obligatorio').max(150),
  quantity: z.number().int().positive('La cantidad debe ser un entero positivo'),
  unitPrice: z.number().positive('El precio unitario debe ser positivo'),
  taxRate: z.number().min(0).max(1).default(0.19), // Porcentaje decimal
});

export const CreateInvoiceSchema = z.object({
  clientName: z.string().min(2, 'El nombre del cliente debe tener al menos 2 caracteres').max(200),
  clientTaxId: z.string().min(5, 'Identificación fiscal inválida').max(30),
  clientEmail: z.string().email('Formato de correo electrónico inválido'),
  items: z.array(CreateInvoiceItemSchema).min(1, 'Debe incluir al menos un producto'),
});

export const LoginSchema = z.object({
  email: z.string().email('Correo inválido'),
  password: z.string().min(8, 'La contraseña debe tener al menos 8 caracteres'),
});

export type CreateInvoiceInput = z.infer<typeof CreateInvoiceSchema>;
export type LoginInput = z.infer<typeof LoginSchema>;
