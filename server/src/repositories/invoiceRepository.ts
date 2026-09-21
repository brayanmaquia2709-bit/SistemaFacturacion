/**
 * REQUERIMIENTO 2: Protección contra Inyección SQL
 * Repositorio de persistencia que utiliza estrictamente CONSULTAS PREPARADAS / PARAMETRIZADAS ($1, $2, ...)
 * o un ORM (Prisma/TypeORM). Bajo ninguna circunstancia concatena cadenas directas en SQL.
 */

export interface DbInvoiceRow {
  id: string;
  invoice_number: string;
  client_name: string;
  client_tax_id: string;
  client_email: string;
  subtotal: number;
  total_tax: number;
  grand_total: number;
  status: string;
  created_by_user_id: string;
  created_at: Date;
}

export class InvoiceRepository {
  // Simulación de pool de conexiones (ej. pg Pool o cliente ORM)
  private dbPool: any;

  constructor(poolInstance: any) {
    this.dbPool = poolInstance;
  }

  /**
   * Inserta una factura en la base de datos usando marcadores de posición ($1...$9)
   */
  async createInvoice(invoiceData: {
    id: string;
    invoiceNumber: string;
    clientName: string;
    clientTaxId: string;
    clientEmail: string;
    subtotal: number;
    totalTax: number;
    grandTotal: number;
    createdByUserId: string;
  }): Promise<DbInvoiceRow> {
    const query = `
      INSERT INTO invoices (
        id, invoice_number, client_name, client_tax_id, 
        client_email, subtotal, total_tax, grand_total, 
        status, created_by_user_id, created_at
      )
      VALUES ($1, $2, $3, $4, $5, $6, $7, $8, 'PENDING', $9, NOW())
      RETURNING *;
    `;

    // LOS VALORES VAN SEPARADOS DE LA SENTENCIA SQL
    const values = [
      invoiceData.id,
      invoiceData.invoiceNumber,
      invoiceData.clientName,
      invoiceData.clientTaxId,
      invoiceData.clientEmail,
      invoiceData.subtotal,
      invoiceData.totalTax,
      invoiceData.grandTotal,
      invoiceData.createdByUserId,
    ];

    // Ejecución segura parametrizada:
    // const result = await this.dbPool.query(query, values);
    // return result.rows[0];

    // Retorno simulado para la arquitectura de muestra:
    return {
      id: invoiceData.id,
      invoice_number: invoiceData.invoiceNumber,
      client_name: invoiceData.clientName,
      client_tax_id: invoiceData.clientTaxId,
      client_email: invoiceData.clientEmail,
      subtotal: invoiceData.subtotal,
      total_tax: invoiceData.totalTax,
      grand_total: invoiceData.grandTotal,
      status: 'PENDING',
      created_by_user_id: invoiceData.createdByUserId,
      created_at: new Date(),
    };
  }

  /**
   * Consulta parametrizada por ID (Evita 'OR 1=1')
   */
  async findById(invoiceId: string): Promise<DbInvoiceRow | null> {
    const query = `SELECT * FROM invoices WHERE id = $1 LIMIT 1;`;
    const values = [invoiceId];

    // const result = await this.dbPool.query(query, values);
    // return result.rows[0] || null;
    return null;
  }
}
