import React, { useState } from 'react';
import { Invoice } from '../types/billing';
import { formatCurrency } from '../utils/sanitizer';

interface PaymentCheckoutModalProps {
  invoice: Invoice;
  isOpen: boolean;
  onClose: () => void;
  onPaymentSuccess: (invoiceId: string) => void;
}

export const PaymentCheckoutModal: React.FC<PaymentCheckoutModalProps> = ({
  invoice,
  isOpen,
  onClose,
  onPaymentSuccess,
}) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<'CARD' | 'PSE' | 'NEQUI'>('CARD');

  if (!isOpen) return null;

  const handleSimulatePayment = (e: React.FormEvent) => {
    e.preventDefault();
    setIsProcessing(true);

    // CIBERSEGURIDAD / PCI-DSS:
    // En una integración real con Stripe / Wompi / Mercado Pago:
    // 1. El formulario tokeniza la tarjeta enviándola DIRECTAMENTE al dominio de la pasarela.
    // 2. La pasarela retorna un ClientSecret o Token de pago de 1 solo uso.
    // 3. El servidor backend confirma la transacción llamando a la API de la pasarela.

    setTimeout(() => {
      setIsProcessing(false);
      onPaymentSuccess(invoice.id);
      alert(`Pago de la factura ${invoice.invoiceNumber} procesado exitosamente mediante ${paymentMethod}.`);
      onClose();
    }, 1500);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">
        
        {/* Encabezado del Modal */}
        <div className="bg-slate-900 text-white p-5 flex justify-between items-center">
          <div>
            <h3 className="font-bold text-base">Pasarela de Pago Segura</h3>
            <p className="text-xs text-slate-400">Factura N° {invoice.invoiceNumber}</p>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-white font-bold text-lg">×</button>
        </div>

        {/* Contenido del Modal */}
        <form onSubmit={handleSimulatePayment} className="p-6 space-y-5">
          
          <div className="bg-indigo-50/80 border border-indigo-100 p-4 rounded-xl flex justify-between items-center">
            <div>
              <p className="text-xs text-indigo-700 font-semibold">Monto Total a Pagar</p>
              <p className="text-2xl font-extrabold text-indigo-950 font-mono">{formatCurrency(invoice.grandTotal)}</p>
            </div>
            <span className="text-[10px] font-bold tracking-wider uppercase bg-indigo-200 text-indigo-800 px-2.5 py-1 rounded-md">
              USD / COP
            </span>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 mb-2">Seleccione Método de Pago</label>
            <div className="grid grid-cols-3 gap-2">
              <button
                type="button"
                onClick={() => setPaymentMethod('CARD')}
                className={`py-2 px-3 text-xs font-semibold rounded-lg border transition ${
                  paymentMethod === 'CARD'
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
                    : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                }`}
              >
                💳 Tarjeta
              </button>
              <button
                type="button"
                onClick={() => setPaymentMethod('PSE')}
                className={`py-2 px-3 text-xs font-semibold rounded-lg border transition ${
                  paymentMethod === 'PSE'
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
                    : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                }`}
              >
                🏦 PSE
              </button>
              <button
                type="button"
                onClick={() => setPaymentMethod('NEQUI')}
                className={`py-2 px-3 text-xs font-semibold rounded-lg border transition ${
                  paymentMethod === 'NEQUI'
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
                    : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                }`}
              >
                📱 Nequi / App
              </button>
            </div>
          </div>

          {/* Formulario Tokenizado Simulado */}
          <div className="space-y-3 pt-2">
            <div>
              <label className="block text-[11px] font-semibold text-slate-600 mb-1">Titular del Pago</label>
              <input
                type="text"
                required
                defaultValue={invoice.clientName}
                className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>

            {paymentMethod === 'CARD' && (
              <div>
                <label className="block text-[11px] font-semibold text-slate-600 mb-1">Número de Tarjeta (PCI-DSS Tokenized)</label>
                <input
                  type="text"
                  required
                  placeholder="•••• •••• •••• 4242"
                  className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                />
              </div>
            )}
          </div>

          <div className="pt-3 border-t border-slate-100 flex items-center justify-between gap-3">
            <span className="text-[10px] text-slate-400 font-medium">🔒 Conexión Cifrada SSL (256-bit)</span>
            <button
              type="submit"
              disabled={isProcessing}
              className="bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold py-2.5 px-5 rounded-xl shadow transition disabled:opacity-50"
            >
              {isProcessing ? 'Procesando Pago...' : 'Confirmar y Pagar'}
            </button>
          </div>

        </form>

      </div>
    </div>
  );
};
