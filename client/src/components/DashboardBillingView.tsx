import React from 'react';
import { formatCurrency } from '../utils/sanitizer';

export const DashboardBillingView: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-100 flex font-sans text-slate-800">
      
      {/* 1. BARRA LATERAL (SIDEBAR - AZUL MARINO / OSCURO) */}
      <aside className="w-64 bg-slate-900 text-slate-300 flex flex-col justify-between p-5 hidden md:flex">
        <div>
          {/* Logo Empresa */}
          <div className="flex items-center gap-3 mb-8 px-2">
            <div className="w-8 h-8 rounded-lg bg-emerald-500 text-white font-black flex items-center justify-center text-sm shadow">
              F
            </div>
            <span className="font-extrabold text-white text-base tracking-wider">INVOICE FLOW</span>
          </div>

          {/* Menú de Navegación Vertical */}
          <nav className="space-y-1 text-xs font-semibold">
            <a href="#dashboard" className="flex items-center gap-3 px-3 py-2.5 rounded-xl bg-slate-800 text-white border-l-4 border-emerald-500">
              📊 Dashboard
            </a>
            <a href="#billing" className="flex items-center gap-3 px-3 py-2.5 rounded-xl hover:bg-slate-800/60 transition">
              🌐 Global Billing
            </a>
            <a href="#clients" className="flex items-center gap-3 px-3 py-2.5 rounded-xl hover:bg-slate-800/60 transition">
              👥 Client List
            </a>
            <a href="#generate" className="flex items-center gap-3 px-3 py-2.5 rounded-xl hover:bg-slate-800/60 transition">
              📄 Generate Invoice
            </a>
            <a href="#settings" className="flex items-center gap-3 px-3 py-2.5 rounded-xl hover:bg-slate-800/60 transition">
              ⚙️ Settings
            </a>
          </nav>
        </div>

        {/* Estado de Seguridad del Servidor */}
        <div className="bg-slate-800/80 p-3 rounded-xl border border-slate-700/50 text-[11px] space-y-1">
          <p className="font-bold text-emerald-400">🔒 System Secure</p>
          <p className="text-slate-400">Session encrypted with 256-bit AES</p>
        </div>
      </aside>

      {/* 2. PANEL PRINCIPAL */}
      <main className="flex-1 p-6 space-y-6 overflow-y-auto">
        
        {/* BARRA SUPERIOR (HEADER) */}
        <header className="flex justify-between items-center bg-white p-4 rounded-xl shadow-sm border border-slate-200">
          <div className="relative w-72">
            <span className="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400 text-xs">🔍</span>
            <input
              type="text"
              placeholder="Search clients, invoices..."
              className="w-full text-xs bg-slate-50 border border-slate-200 pl-9 pr-4 py-2 rounded-lg outline-none focus:bg-white focus:ring-1 focus:ring-emerald-500"
            />
          </div>

          <div className="flex items-center gap-4">
            <span className="hidden sm:inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-800 border border-emerald-200">
              ● Secure Access Active
            </span>
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-slate-900 text-white font-bold flex items-center justify-center text-xs">
                SJ
              </div>
              <span className="text-xs font-bold text-slate-800">Sarah Jenkins</span>
            </div>
          </div>
        </header>

        {/* CONTENIDO DIVIDIDO (CENTRAL + SECUNDARIO) */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

          {/* ÁREA CENTRAL (8 COLS): TARJETAS TOP + TABLA DE CLIENTES */}
          <div className="lg:col-span-8 space-y-6">
            
            {/* TARJETAS DE RESUMEN (TOP CARDS) CON CANDADOS */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm flex justify-between items-start">
                <div>
                  <p className="text-xs font-semibold text-slate-500">Total Receivables</p>
                  <h3 className="text-2xl font-bold text-slate-900 mt-1 font-mono">{formatCurrency(34500)}</h3>
                  <p className="text-[11px] text-emerald-600 font-bold mt-1">↑ Up 12.5% this month</p>
                </div>
                <span className="p-2 bg-slate-100 rounded-lg text-slate-500 text-xs">🔒</span>
              </div>

              <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm flex justify-between items-start">
                <div>
                  <p className="text-xs font-semibold text-slate-500">Paid This Month</p>
                  <h3 className="text-2xl font-bold text-slate-900 mt-1 font-mono">{formatCurrency(21800)}</h3>
                  <p className="text-[11px] text-slate-500 mt-1">89 Verified Invoices</p>
                </div>
                <span className="p-2 bg-emerald-50 rounded-lg text-emerald-600 text-xs">✓</span>
              </div>
            </div>

            {/* TABLA DE DATOS (CLIENT LIST) */}
            <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
              <div className="px-5 py-4 border-b border-slate-100 flex justify-between items-center">
                <h3 className="text-sm font-bold text-slate-900">Client Invoices</h3>
                <button className="px-3 py-1 text-xs font-semibold text-white bg-slate-900 rounded-lg">
                  + Create Invoice
                </button>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left text-slate-700">
                  <thead className="bg-slate-50 uppercase text-[10px] text-slate-500 font-semibold border-b border-slate-200">
                    <tr>
                      <th className="px-5 py-3">Client</th>
                      <th className="px-5 py-3">Invoice ID</th>
                      <th className="px-5 py-3">Date</th>
                      <th className="px-5 py-3 text-right">Amount</th>
                      <th className="px-5 py-3 text-center">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    <tr className="hover:bg-slate-50">
                      <td className="px-5 py-3.5 font-bold text-slate-900">Acme Corp</td>
                      <td className="px-5 py-3.5 font-mono text-slate-500">#00000001</td>
                      <td className="px-5 py-3.5 text-slate-500">Oct 18, 2026</td>
                      <td className="px-5 py-3.5 text-right font-bold text-slate-900 font-mono">{formatCurrency(340)}</td>
                      <td className="px-5 py-3.5 text-center">
                        <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">Paid</span>
                      </td>
                    </tr>
                    <tr className="hover:bg-slate-50">
                      <td className="px-5 py-3.5 font-bold text-slate-900">Globex LLC</td>
                      <td className="px-5 py-3.5 font-mono text-slate-500">#00000002</td>
                      <td className="px-5 py-3.5 text-slate-500">Oct 18, 2026</td>
                      <td className="px-5 py-3.5 text-right font-bold text-slate-900 font-mono">{formatCurrency(450)}</td>
                      <td className="px-5 py-3.5 text-center">
                        <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800">Pending</span>
                      </td>
                    </tr>
                    <tr className="hover:bg-slate-50">
                      <td className="px-5 py-3.5 font-bold text-slate-900">Stellar Solutions</td>
                      <td className="px-5 py-3.5 font-mono text-slate-500">#00000003</td>
                      <td className="px-5 py-3.5 text-slate-500">Oct 17, 2026</td>
                      <td className="px-5 py-3.5 text-right font-bold text-slate-900 font-mono">{formatCurrency(230)}</td>
                      <td className="px-5 py-3.5 text-center">
                        <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">Paid</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

          </div>

          {/* PANEL SECUNDARIO (4 COLS): APROBACIONES PENDIENTES Y GRÁFICO REVENUE */}
          <div className="lg:col-span-4 space-y-6">
            
            {/* APROBACIONES PENDIENTES */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-3">
              <h4 className="text-xs font-bold text-slate-900 border-b border-slate-100 pb-2">
                Pending Approvals
              </h4>
              <div className="space-y-2 text-xs">
                <div className="p-3 bg-amber-50/60 rounded-lg border border-amber-100 flex justify-between items-center">
                  <div>
                    <p className="font-bold text-amber-900">Invoice #00000002</p>
                    <p className="text-[11px] text-amber-700">Requires Admin Audit</p>
                  </div>
                  <span className="font-bold text-amber-900 font-mono">{formatCurrency(450)}</span>
                </div>
              </div>
            </div>

            {/* GRÁFICO DE INGRESOS (MINIMALIST BAR CHART) */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-4">
              <h4 className="text-xs font-bold text-slate-900">Monthly Revenue</h4>
              <div className="h-36 flex items-end justify-between gap-2 pt-4 px-2 border-b border-slate-100">
                <div className="w-full bg-emerald-200 rounded-t h-[40%]" title="May: $14k"></div>
                <div className="w-full bg-emerald-300 rounded-t h-[60%]" title="Jun: $19k"></div>
                <div className="w-full bg-emerald-400 rounded-t h-[50%]" title="Jul: $16k"></div>
                <div className="w-full bg-emerald-500 rounded-t h-[75%]" title="Aug: $24k"></div>
                <div className="w-full bg-emerald-600 rounded-t h-[90%]" title="Sep: $29k"></div>
                <div className="w-full bg-emerald-700 rounded-t h-[100%]" title="Oct: $34k"></div>
              </div>
              <div className="flex justify-between text-[10px] text-slate-400 font-semibold px-1">
                <span>May</span><span>Jun</span><span>Jul</span><span>Aug</span><span>Sep</span><span>Oct</span>
              </div>
            </div>

          </div>

        </div>

      </div>

    </div>
  );
};
