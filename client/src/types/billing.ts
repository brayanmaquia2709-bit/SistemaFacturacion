export type UserRole = 'ADMIN' | 'EMPLOYEE';

export interface UserSession {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}

export interface InvoiceItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  taxRate: number; // Porcentaje de IVA, ej: 19% -> 0.19 o 19
  subtotal: number;
  taxAmount: number;
  total: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  clientName: string;
  clientTaxId: string; // NIT / RUTC / RFC
  clientEmail: string;
  issueDate: string;
  dueDate: string;
  status: 'PAID' | 'PENDING' | 'CANCELLED';
  items: InvoiceItem[];
  subtotal: number;
  totalTax: number;
  grandTotal: number;
  createdByName: string;
}
