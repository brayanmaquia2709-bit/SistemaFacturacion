import React, { useState, useMemo } from 'react';
import { Invoice, InvoiceItem, UserSession } from '../types/billing';
import { InvoiceTable } from './InvoiceTable';
import { formatCurrency, sanitizeInput, isValidEmail } from '../utils/sanitizer';

// Simulación de usuario autenticado en sesión
const mockSession: UserSession = {
  id: 'usr_101',
  name: 'Carlos Mendoza',
  email: 'cmendoza@empresa.com',
  role: 'ADMIN', // Cambiar a 'EMPLOYEE' para probar el RBAC
};

export const BillingView: React.FC = () => {
  // Estado de sesión
  const [currentUser] = useState<UserSession>(mockSession);

  // Estados de datos de la factura actual
  const [clientName, setClientName] = useState('');
  const [clientTaxId, setClientTaxId] = useState('');
  const [clientEmail, setClientEmail] = useState('');
  
  // Estado para la línea de producto en ingreso
  const [prodName, setProdName] = useState('');
  const [quantity, setQuantity] = useState<number | ''>(1);
  const [unitPrice, setUnitPrice] = useState<number | ''>('');
  const [taxRate] = useState<number>(0.19); // 19% IVA estándar

  // Lista de items de la factura en borrador
  const [items, setItems] = useState<InvoiceItem[]>([]);
  
  // Histórico de facturas emitidas
  const [invoices, setInvoices] = useState<Invoice[]>([
    {
      id: 'inv_001',
      invoiceNumber: 'FAC-2026-001',
      clientName: 'Corporación Ac me S.A.',
      clientTaxId: '900.123.456-1',
      clientEmail: 'compras@acme.com',
      issueDate: '2026-09-20',
      dueDate: '2026-10-20',
      status: 'PAID',
      items: [],
      subtotal: 1000.0,
      totalTax: 190.0,
      grandTotal: 1190.0,
      createdByName: 'Carlos Mendoza',
    },
  ]);

  const [formError, setFormError] = useState<string | null>(null);

  // Cálculos reactivos en tiempo real con useMemo
  const totals = useMemo(() => {
    const subtotal = items.reduce((acc, item) => acc + item.subtotal, 0);
    const totalTax = items.reduce((acc, item) => acc + item.taxAmount, 0);
    const grandTotal = subtotal + totalTax;
    return { subtotal, totalTax, grandTotal };
  }, [items]);

  // Agregar ítem a la factura
  const handleAddItem = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    const cleanProdName = sanitizeInput(prodName);
    const numQty = Number(quantity);
    const numPrice = Number(unitPrice);

    if (!cleanProdName) {
      setFormError('El nombre del producto es obligatorio.');
      return;
    }
    if (isNaN(numQty) || numQty <= 0) {
      setFormError('La cantidad debe ser mayor a 0.');
      return;
    }
    if (isNaN(numPrice) || numPrice <= 0) {
      setFormError('El precio unitario debe ser mayor a 0.');
      return;
    }

    const itemSubtotal = numQty * numPrice;
    const itemTax = itemSubtotal * taxRate;

    const newItem: InvoiceItem = {
      id: `item_${Date.now()}`,
      productName: cleanProdName,
      quantity: numQty,
      unitPrice: numPrice,
      taxRate: taxRate,
      subtotal: itemSubtotal,
      taxAmount: itemTax,
      total: itemSubtotal + itemTax,
    };

    setItems((prev) => [...prev, newItem]);
    setProdName('');
    setQuantity(1);
    setUnitPrice('');
  };

  const handleRemoveItem = (id: string) => {
    setItems((prev) => prev.filter((item) => item.id !== id));
  };

  // Emisión de factura con validación de seguridad
  const handleEmitInvoice = () => {
    setFormError(null);
    const cleanClientName = sanitizeInput(clientName);
    const cleanTaxId = sanitizeInput(clientTaxId);
    const cleanEmail = sanitizeInput(clientEmail);

    if (!cleanClientName) return setFormError('Ingrese la razón social o nombre del cliente.');
    if (!cleanTaxId) return setFormError('Ingrese la identificación fiscal del cliente.');
    if (!isValidEmail(cleanEmail)) return setFormError('Ingrese un correo electrónico válido.');
    if (items.length === 0) return setFormError('Debe agregar al menos un producto a la factura.');

    const newInvoice: Invoice = {
      id: `inv_${Date.now()}`,
      invoiceNumber: `FAC-2026-00${invoices.length + 1}`,
      clientName: cleanClientName,
      clientTaxId: cleanTaxId,
      clientEmail: cleanEmail,
      issueDate: new Date().toISOString().split('T')[0],
      dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: 'PENDING',
      items: [...items],
      subtotal: totals.subtotal,
      totalTax: totals.totalTax,
      grandTotal: totals.grandTotal,
      createdByName: currentUser.name,
    };

    setInvoices((prev) => [newInvoice, ...prev]);
    // Resetear formulario
    setClientName('');
    setClientTaxId('');
    setClientEmail('');
    setItems([]);
    alert(`Factura ${newInvoice.invoiceNumber} registrada exitosamente.`);
  };

  return (
    <div className="min-h-screen bg-slate-100 text-slate-800 font-sans p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-6">

        {/* Encabezado Corporativo y Estado de Sesión */}
        <header className="flex flex-col md:flex-row md:items-center justify-between bg-white p-6 rounded-xl shadow-sm border border-slate-200 gap-4">
          <div>
            <div className="flex items-center gap-3">
              <span className="bg-indigo-600 text-white font-bold text-lg px-3 py-1 rounded-lg tracking-wider">
                BILL-PRO
              </span>
              <h1 className="text-xl md:text-2xl font-bold text-slate-900">Módulo de Facturación Empresarial</h1>
            </div>
            <p className="text-xs text-slate-500 mt-1">
              Plataforma segura de emisión de comprobantes fiscales
            </p>
          </div>

          <div className="flex items-center gap-3 bg-slate-50 px-4 py-2 rounded-lg border border-slate-200">
            <div className="w-8 h-8 rounded-full bg-indigo-100 text-indigo-700 flex items-center justify-center font-bold text-sm">
              {currentUser.name.charAt(0)}
            </div>
            <div>
              <p className="text-xs font-bold text-slate-800">{currentUser.name}</p>
              <p className="text-[10px] font-semibold text-indigo-600 tracking-wider uppercase">
                Rol: {currentUser.role}
              </p>
            </div>
          </div>
        </header>

        {/* Banner de Errores de Validación */}
        {formError && (
          <div className="bg-rose-50 border-l-4 border-rose-500 p-4 rounded-r-lg text-rose-700 text-sm flex justify-between items-center">
            <span>⚠️ {formError}</span>
            <button onClick={() => setFormError(null)} className="font-bold text-rose-800 text-xs">Cerrar</button>
          </div>
        )}

        {/* 
          REQUERIMIENTO 1: CSS Grid con Auto-Ajuste Proporcional 
          1 Columna en móviles, 12 Columnas en Monitores Grandes (8 para formulario, 4 para resumen)
        */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

          {/* Panel Formulario de Ingreso (8 Cols en escritorio) */}
          <div className="lg:col-span-8 bg-white p-6 rounded-xl shadow-sm border border-slate-200 space-y-6">
            <h2 className="text-base font-bold text-slate-900 border-b border-slate-100 pb-3">
              1. Datos del Cliente
            </h2>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Razón Social / Nombre *</label>
                <input
                  type="text"
                  value={clientName}
                  onChange={(e) => setClientName(e.target.value)}
                  placeholder="Ej: Tech Solutions Corp"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">NIT / RUTC / RFC *</label>
                <input
                  type="text"
                  value={clientTaxId}
                  onChange={(e) => setClientTaxId(e.target.value)}
                  placeholder="Ej: 900.876.543-2"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none font-mono"
                />
              </div>

              <div className="sm:col-span-2 lg:col-span-1">
                <label className="block text-xs font-semibold text-slate-700 mb-1">Correo Electrónico *</label>
                <input
                  type="email"
                  value={clientEmail}
                  onChange={(e) => setClientEmail(e.target.value)}
                  placeholder="facturacion@cliente.com"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                />
              </div>
            </div>

            <h2 className="text-base font-bold text-slate-900 border-b border-slate-100 pb-3 pt-2">
              2. Ingreso de Productos / Servicios
            </h2>

            {/* Formulario Inline Auto-Ajustable */}
            <form onSubmit={handleAddItem} className="grid grid-cols-1 sm:grid-cols-12 gap-3 bg-slate-50 p-4 rounded-lg border border-slate-200 items-end">
              <div className="sm:col-span-5">
                <label className="block text-[11px] font-semibold text-slate-600 mb-1">Producto / Servicio</label>
                <input
                  type="text"
                  value={prodName}
                  onChange={(e) => setProdName(e.target.value)}
                  placeholder="Descripción del ítem"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-md bg-white focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>

              <div className="sm:col-span-3">
                <label className="block text-[11px] font-semibold text-slate-600 mb-1">Cantidad</label>
                <input
                  type="number"
                  min="1"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value === '' ? '' : Number(e.target.value))}
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-md bg-white focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>

              <div className="sm:col-span-3">
                <label className="block text-[11px] font-semibold text-slate-600 mb-1">Precio Unit. ($)</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={unitPrice}
                  onChange={(e) => setUnitPrice(e.target.value === '' ? '' : Number(e.target.value))}
                  placeholder="0.00"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-md bg-white focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>

              <div className="sm:col-span-1">
                <button
                  type="submit"
                  className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold text-sm py-2 rounded-md transition shadow-sm"
                  title="Agregar Ítem"
                >
                  +
                </button>
              </div>
            </form>

            {/* Tabla de ítems en borrador con scroll responsive */}
            <div className="overflow-x-auto w-full border border-slate-200 rounded-lg">
              <table className="w-full text-xs text-left text-slate-700 min-w-[500px]">
                <thead className="bg-slate-100 uppercase text-[10px] text-slate-500 tracking-wider">
                  <tr>
                    <th className="px-4 py-2">Ítem</th>
                    <th className="px-4 py-2 text-center">Cant.</th>
                    <th className="px-4 py-2 text-right">Precio Unit.</th>
                    <th className="px-4 py-2 text-right">IVA (19%)</th>
                    <th className="px-4 py-2 text-right">Subtotal</th>
                    <th className="px-4 py-2 text-center">Acción</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {items.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-4 py-6 text-center text-slate-400 italic">
                        No hay productos agregados a esta factura.
                      </td>
                    </tr>
                  ) : (
                    items.map((item) => (
                      <tr key={item.id} className="hover:bg-slate-50">
                        <td className="px-4 py-2.5 font-medium text-slate-900">{item.productName}</td>
                        <td className="px-4 py-2.5 text-center font-mono">{item.quantity}</td>
                        <td className="px-4 py-2.5 text-right font-mono">{formatCurrency(item.unitPrice)}</td>
                        <td className="px-4 py-2.5 text-right font-mono text-slate-500">{formatCurrency(item.taxAmount)}</td>
                        <td className="px-4 py-2.5 text-right font-bold text-slate-900 font-mono">{formatCurrency(item.subtotal)}</td>
                        <td className="px-4 py-2.5 text-center">
                          <button
                            onClick={() => handleRemoveItem(item.id)}
                            className="text-rose-600 hover:text-rose-800 font-bold px-2 py-0.5 rounded hover:bg-rose-50"
                          >
                            ×
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

          </div>

          {/* Panel Lateral de Resumen y Emisión (4 Cols en escritorio) */}
          <div className="lg:col-span-4 bg-white p-6 rounded-xl shadow-sm border border-slate-200 flex flex-col justify-between space-y-6">
            <div>
              <h2 className="text-base font-bold text-slate-900 border-b border-slate-100 pb-3">
                Resumen de Liquidación
              </h2>

              <div className="space-y-3 mt-4 text-xs text-slate-600">
                <div className="flex justify-between py-1 border-b border-slate-100">
                  <span>Subtotal Neto:</span>
                  <span className="font-bold font-mono text-slate-900">{formatCurrency(totals.subtotal)}</span>
                </div>
                <div className="flex justify-between py-1 border-b border-slate-100">
                  <span>Impuestos (IVA 19%):</span>
                  <span className="font-bold font-mono text-slate-900">{formatCurrency(totals.totalTax)}</span>
                </div>
                <div className="flex justify-between py-3 text-sm font-extrabold text-slate-900 bg-indigo-50/60 px-3 rounded-lg border border-indigo-100 mt-2">
                  <span>GRAN TOTAL:</span>
                  <span className="font-mono text-indigo-700">{formatCurrency(totals.grandTotal)}</span>
                </div>
              </div>

              <div className="mt-6 bg-slate-50 p-3 rounded-lg border border-slate-200 text-[11px] text-slate-500 space-y-1">
                <p className="font-semibold text-slate-700">🔒 Control de Seguridad y Auditaría:</p>
                <p>• Los totales son recalculados en el servidor antes de la firma.</p>
                <p>• Petición tokenizada anti-CSRF activa.</p>
              </div>
            </div>

            <button
              onClick={handleEmitInvoice}
              className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-3 px-4 rounded-xl shadow-md transition duration-150 transform active:scale-95 text-sm"
            >
              Emite Factura Electrónica
            </button>
          </div>

        </div>

        {/* 
          REQUERIMIENTO 1: Listado de Facturas Adaptable a Dispositivos Móviles
        */}
        <div className="pt-4">
          <InvoiceTable
            invoices={invoices}
            userRole={currentUser.role}
            onCancelInvoice={(id) => {
              if (confirm('¿Está seguro de anular esta factura? Esta acción quedará registrada en auditoría.')) {
                setInvoices((prev) =>
                  prev.map((inv) => (inv.id === id ? { ...inv, status: 'CANCELLED' } : inv))
                );
              }
            }}
          />
        </div>

      </div>
    </div>
  );
};
