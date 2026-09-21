import React, { useState } from 'react';

export const LoginView: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    alert(`Iniciando sesión segura para ${username}...`);
  };

  return (
    <div className="min-h-screen bg-slate-100 flex items-center justify-center p-4 font-sans text-slate-800">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
        
        {/* ENCABEZADO */}
        <div className="p-8 text-center border-b border-slate-100">
          <div className="w-12 h-12 rounded-xl bg-emerald-600 text-white flex items-center justify-center mx-auto text-xl font-bold mb-3 shadow-md shadow-emerald-200">
            🛡️
          </div>
          <h2 className="text-xl font-bold text-slate-900">Secure Log-In</h2>
          <p className="text-xs text-slate-500 mt-1">Global Corporate Billing Portal</p>
        </div>

        {/* FORMULARIO DE INGRESO */}
        <form onSubmit={handleSubmit} className="p-8 space-y-4">
          
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Username or Employee ID</label>
            <input
              type="text"
              required
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="e.g. emp_10293"
              className="w-full text-xs px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg outline-none focus:bg-white focus:ring-2 focus:ring-emerald-500 transition"
            />
          </div>

          <div>
            <div className="flex justify-between items-center mb-1">
              <label className="block text-xs font-semibold text-slate-700">Password</label>
              <a href="#forgot" className="text-[11px] font-semibold text-emerald-700 hover:underline">Forgot password?</a>
            </div>
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••••••"
              className="w-full text-xs px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg outline-none focus:bg-white focus:ring-2 focus:ring-emerald-500 transition"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">2FA Authenticator Code</label>
            <input
              type="text"
              maxLength={6}
              value={twoFactorCode}
              onChange={(e) => setTwoFactorCode(e.target.value)}
              placeholder="6-digit code (e.g. 849201)"
              className="w-full text-xs px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg outline-none focus:bg-white focus:ring-2 focus:ring-emerald-500 transition font-mono tracking-widest text-center"
            />
          </div>

          {/* BOTÓN FOCAL PRINCIPAL VERDE ESMERALDA */}
          <button
            type="submit"
            className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-sm py-3 rounded-xl shadow-lg shadow-emerald-600/30 transition duration-150 transform active:scale-95 mt-2"
          >
            🔒 Secure Sign In
          </button>

          {/* ELEMENTOS DE SEGURIDAD & COMPLIANCE */}
          <div className="pt-4 border-t border-slate-100 text-center space-y-2">
            <p className="text-[11px] text-slate-500 flex items-center justify-center gap-1.5 font-medium">
              <span>🔐</span> Two Factor Authentication (2FA) required
            </p>
            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-800 border border-emerald-200">
              ✓ Security Certified (256-Bit SSL Encryption)
            </span>
          </div>

        </form>

      </div>
    </div>
  );
};
