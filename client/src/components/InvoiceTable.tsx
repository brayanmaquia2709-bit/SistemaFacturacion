import React from 'react';
import { Invoice } from '../types/billing';
import { formatCurrency, sanitizeInput } from '../utils/sanitizer';

interface InvoiceTableProps {
  invoices: Invoice[];
  userRole: 'ADMIN' | 'EMPLOYEE';
  onCancelInvoice?: (id: string) => void;
  onViewDetails?: (invoice: Invoice) => void;
}

export const InvoiceTable: React.FC<InvoiceTableProps> = ({
  invoices,
  userRole,
  onCancelInvoice,
  onViewDetails,
}) => {
  const getStatusBadge = (status: Invoice['status']) => {
    switch (status) {
      case 'PAID':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            <span className="w-1.5 h-1.5 mr-1.5 rounded-full bg-emerald-500"></span>
            Pagada
          </span>
        );
      case 'PENDING':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            <span className="w-1.5 h-1.5 mr-1.5 rounded-full bg-amber-500"></span>
            Pendiente
          </span>
        );
      case 'CANCELLED':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-800 border border-rose-200">
            <span className="w-1.5 h-1.5 mr-1.5 rounded-full bg-rose-500"></span>
            Anulada
          </span>
        );
    }
  };

  return (
    <div className="w-full bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 bg-slate-50/50">
        <div>
          <h3 className="text-lg font-bold text-slate-900">Histórico de Facturación</h3>
          <p className="text-xs text-slate-500">Listado general de comprobantes emitidos en el sistema</p>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500 font-medium">Total: {invoices.length} facturas</span>
        </div>
      </div>

      {/* 
        REQUERIMIENTO 1: Contenedor con overflow-x-auto para garantizar scroll horizontal 
        interno en móviles sin romper el diseño responsive global.
      */}
      <div className="overflow-x-auto w-full">
        <table className="w-full text-left text-sm border-collapse min-w-[768px]">
          <thead>
            <tr className="bg-slate-100/80 text-slate-700 uppercase font-semibold text-[11px] tracking-wider border-b border-slate-200">
              <th scope="col" className="px-6 py-3.5">N° Factura</th>
              <th scope="col" className="px-6 py-3.5">Cliente</th>
              <th scope="col" className="px-6 py-3.5">Identificación</th>
              <th scope="col" className="px-6 py-3.5">Fecha Emisión</th>
              <th scope="col" className="px-6 py-3.5">Estado</th>
              <th scope="col" className="px-6 py-3.5 text-right">Monto Total</th>
              <th scope="col" className="px-6 py-3.5 text-center">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 font-normal text-slate-700">
            {invoices.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-10 text-center text-slate-400">
                  No hay facturas registradas en este período.
                </td>
              </tr>
            ) : (
              invoices.map((inv) => (
                <tr key={inv.id} className="hover:bg-slate-50/80 transition-colors duration-150">
                  <td className="px-6 py-4 font-semibold text-slate-900 whitespace-nowrap">
                    {sanitizeInput(inv.invoiceNumber)}
                  </td>
                  <td className="px-6 py-4 font-medium text-slate-800 whitespace-nowrap">
                    {sanitizeInput(inv.clientName)}
                  </td>
                  <td className="px-6 py-4 text-slate-500 text-xs whitespace-nowrap font-mono">
                    {sanitizeInput(inv.clientTaxId)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-slate-600 text-xs">
                    {inv.issueDate}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {getStatusBadge(inv.status)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right font-bold text-slate-900">
                    {formatCurrency(inv.grandTotal)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <div className="flex items-center justify-center gap-2">
                      <button
                        onClick={() => onViewDetails?.(inv)}
                        className="px-2.5 py-1 text-xs font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 rounded transition"
                      >
                        Ver Detalle
                      </button>

                      {/* RBAC FRONTEND CONTROL: Solo el ADMIN puede anular facturas */}
                      {userRole === 'ADMIN' && inv.status !== 'CANCELLED' && (
                        <button
                          onClick={() => onCancelInvoice?.(inv.id)}
                          className="px-2.5 py-1 text-xs font-medium text-rose-700 bg-rose-50 hover:bg-rose-100 border border-rose-200 rounded transition"
                        >
                          Anular
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
