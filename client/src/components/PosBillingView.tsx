import React, { useState } from 'react';
import { formatCurrency } from '../utils/sanitizer';

interface PosProduct {
  id: string;
  name: string;
  sku: string;
  price: number;
  category: string;
  stock: number;
  imageIcon: string;
}

interface CartItem extends PosProduct {
  quantity: number;
}

const SAMPLE_PRODUCTS: PosProduct[] = [
  { id: 'p1', name: 'Impresora Térmica POS 80mm', sku: 'IMP-80M', price: 120.00, category: 'Hardware', stock: 15, imageIcon: '🖨️' },
  { id: 'p2', name: 'Lector Código de Barras 2D', sku: 'SCAN-2D', price: 65.50, category: 'Hardware', stock: 28, imageIcon: '🔍' },
  { id: 'p3', name: 'Cajón Monedero RJ11', sku: 'CAJ-RJ11', price: 45.00, category: 'Hardware', stock: 10, imageIcon: '💼' },
  { id: 'p4', name: 'Papel Térmico 80x60mm (Caja x50)', sku: 'PAP-8060', price: 25.00, category: 'Insumos', stock: 100, imageIcon: '📜' },
  { id: 'p5', name: 'Licencia Software POS Anual', sku: 'LIC-POS1Y', price: 299.00, category: 'Software', stock: 999, imageIcon: '⚡' },
  { id: 'p6', name: 'Teclado Mecánico POS Pro', sku: 'TEC-POS', price: 85.00, category: 'Hardware', stock: 12, imageIcon: '⌨️' },
];

export const PosBillingView: React.FC = () => {
  const [cart, setCart] = useState<CartItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('TODOS');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [selectedCustomer, setSelectedCustomer] = useState({
    name: 'Cliente General / Mostrador',
    idNumber: '222222222222',
    type: 'CONSUMIDOR_FINAL',
  });
  const [paymentMethod, setPaymentMethod] = useState<'EFECTIVO' | 'TARJETA' | 'TRANSFERENCIA'>('TARJETA');

  // Filtrado de catálogo
  const filteredProducts = SAMPLE_PRODUCTS.filter((prod) => {
    const matchesCategory = selectedCategory === 'TODOS' || prod.category === selectedCategory;
    const matchesSearch = prod.name.toLowerCase().includes(searchQuery.toLowerCase()) || prod.sku.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesCategory && matchesSearch;
  });

  // Agregar al carrito
  const addToCart = (product: PosProduct) => {
    setCart((prev) => {
      const existing = prev.find((item) => item.id === product.id);
      if (existing) {
        return prev.map((item) =>
          item.id === product.id ? { ...item, quantity: item.quantity + 1 } : item
        );
      }
      return [...prev, { ...product, quantity: 1 }];
    });
  };

  const updateQuantity = (id: string, delta: number) => {
    setCart((prev) =>
      prev
        .map((item) => {
          if (item.id === id) {
            const newQty = item.quantity + delta;
            return newQty > 0 ? { ...item, quantity: newQty } : null;
          }
          return item;
        })
        .filter(Boolean) as CartItem[]
    );
  };

  // Cálculos de la factura
  const subtotal = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const taxAmount = subtotal * 0.19; // 19% IVA
  const grandTotal = subtotal + taxAmount;

  return (
    <div className="min-h-screen bg-[#121214] text-[#F9FAFB] font-sans p-4 md:p-6 select-none">
      
      {/* BARRA SUPERIOR BRANDING POS */}
      <header className="flex justify-between items-center pb-4 mb-4 border-b border-[#2D2D38]">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[#10B981] to-[#059669] flex items-center justify-center text-white font-extrabold text-xl shadow-lg shadow-emerald-950/50">
            ⚡
          </div>
          <div>
            <h1 className="text-lg font-bold tracking-tight text-white">POS ENTERPRISE DARK</h1>
            <p className="text-[11px] text-[#9CA3AF]">Terminal de Punto de Venta | Caja #01</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-[#10B981]/10 text-[#10B981] border border-[#10B981]/20">
            ● SISTEMA EN LÍNEA
          </span>
          <div className="bg-[#1E1E24] px-3 py-1.5 rounded-xl border border-[#2D2D38] text-xs font-medium text-[#9CA3AF]">
            Cajero: <span className="text-white font-bold">Admin POS</span>
          </div>
        </div>
      </header>

      {/* 
        GRID DE 4 ZONAS (DASHBOARD MINIMALISTA POS)
        Columna Izquierda (7 Cols): Zona 1 (Cliente) + Zona 2 (Catálogo / Inventario)
        Columna Derecha (5 Cols): Zona 3 (Carrito / Resumen) + Zona 4 (Pago & Focal Point Total)
      */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">

        {/* COLUMNA IZQUIERDA: CLIENTE + CATÁLOGO */}
        <div className="lg:col-span-7 space-y-5">
          
          {/* ZONA 1: CLIENTE */}
          <section className="bg-[#1E1E24] p-4 rounded-2xl border border-[#2D2D38] shadow-sm flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-[#25252E] flex items-center justify-center text-lg text-[#9CA3AF]">
                👤
              </div>
              <div>
                <p className="text-[10px] font-bold tracking-wider text-[#9CA3AF] uppercase">Cliente Asignado</p>
                <h3 className="text-sm font-bold text-white">{selectedCustomer.name}</h3>
                <p className="text-xs text-[#9CA3AF] font-mono">ID / NIT: {selectedCustomer.idNumber}</p>
              </div>
            </div>

            <button className="px-3.5 py-2 text-xs font-semibold text-[#10B981] bg-[#10B981]/10 hover:bg-[#10B981]/20 rounded-xl border border-[#10B981]/30 transition">
              Cambiar Cliente
            </button>
          </section>

          {/* ZONA 2: CATÁLOGO DE PRODUCTOS / INVENTARIO */}
          <section className="bg-[#1E1E24] p-5 rounded-2xl border border-[#2D2D38] shadow-sm space-y-4">
            
            {/* Buscador & Categorías */}
            <div className="flex flex-col sm:flex-row gap-3 justify-between items-center">
              <div className="relative w-full sm:w-72">
                <span className="absolute inset-y-0 left-0 pl-3 flex items-center text-[#9CA3AF]">🔍</span>
                <input
                  type="text"
                  placeholder="Buscar por nombre o SKU..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full bg-[#121214] text-xs text-white pl-9 pr-4 py-2.5 rounded-xl border border-[#2D2D38] focus:border-[#10B981] focus:outline-none transition"
                />
              </div>

              <div className="flex gap-1.5 overflow-x-auto w-full sm:w-auto pb-1 sm:pb-0">
                {['TODOS', 'Hardware', 'Insumos', 'Software'].map((cat) => (
                  <button
                    key={cat}
                    onClick={() => setSelectedCategory(cat)}
                    className={`px-3 py-1.5 rounded-xl text-xs font-medium whitespace-nowrap transition ${
                      selectedCategory === cat
                        ? 'bg-[#10B981] text-white font-bold'
                        : 'bg-[#25252E] text-[#9CA3AF] hover:text-white border border-[#2D2D38]'
                    }`}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* Grid Táctil de Tarjetas de Productos */}
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 max-h-[460px] overflow-y-auto pr-1">
              {filteredProducts.map((prod) => (
                <button
                  key={prod.id}
                  onClick={() => addToCart(prod)}
                  className="group bg-[#25252E] hover:bg-[#2E2E3A] p-4 rounded-xl border border-[#2D2D38] hover:border-[#10B981]/50 text-left transition flex flex-col justify-between h-36 relative overflow-hidden active:scale-95"
                >
                  <div className="flex justify-between items-start">
                    <span className="text-2xl">{prod.imageIcon}</span>
                    <span className="text-[10px] font-mono bg-[#121214] text-[#9CA3AF] px-2 py-0.5 rounded-md border border-[#2D2D38]">
                      Stock: {prod.stock}
                    </span>
                  </div>

                  <div>
                    <h4 className="text-xs font-bold text-white group-hover:text-[#10B981] line-clamp-1 transition">
                      {prod.name}
                    </h4>
                    <p className="text-[10px] text-[#9CA3AF] font-mono">{prod.sku}</p>
                    <p className="text-sm font-extrabold text-[#10B981] font-mono mt-1">
                      {formatCurrency(prod.price)}
                    </p>
                  </div>
                </button>
              ))}
            </div>

          </section>

        </div>

        {/* COLUMNA DERECHA: CARRITO + PAGO & TOTAL (PUNTO FOCAL) */}
        <div className="lg:col-span-5 space-y-5">
          
          {/* ZONA 3: CARRITO / RESUMEN DE LA FACTURA */}
          <section className="bg-[#1E1E24] p-5 rounded-2xl border border-[#2D2D38] shadow-sm space-y-4">
            <div className="flex justify-between items-center border-b border-[#2D2D38] pb-3">
              <h3 className="text-sm font-bold text-white flex items-center gap-2">
                🛒 Ítems en Carrito
                <span className="bg-[#10B981]/20 text-[#10B981] text-xs font-bold px-2 py-0.5 rounded-full">
                  {cart.reduce((a, c) => a + c.quantity, 0)}
                </span>
              </h3>
              {cart.length > 0 && (
                <button onClick={() => setCart([])} className="text-xs text-[#EF4444] hover:underline">
                  Vaciar
                </button>
              )}
            </div>

            {/* Lista de Ítems */}
            <div className="space-y-2.5 max-h-[240px] overflow-y-auto pr-1">
              {cart.length === 0 ? (
                <div className="py-12 text-center text-[#9CA3AF] text-xs">
                  El carrito está vacío.<br />Haga clic en un producto para agregarlo.
                </div>
              ) : (
                cart.map((item) => (
                  <div
                    key={item.id}
                    className="bg-[#25252E] p-3 rounded-xl border border-[#2D2D38] flex items-center justify-between gap-3"
                  >
                    <div className="flex-1 min-w-0">
                      <h5 className="text-xs font-bold text-white truncate">{item.name}</h5>
                      <p className="text-[11px] text-[#10B981] font-mono">{formatCurrency(item.price)} c/u</p>
                    </div>

                    <div className="flex items-center gap-2">
                      <div className="flex items-center bg-[#121214] border border-[#2D2D38] rounded-lg">
                        <button
                          onClick={() => updateQuantity(item.id, -1)}
                          className="px-2 py-0.5 text-xs text-[#9CA3AF] hover:text-white"
                        >
                          -
                        </button>
                        <span className="px-2 text-xs font-bold text-white font-mono">{item.quantity}</span>
                        <button
                          onClick={() => updateQuantity(item.id, 1)}
                          className="px-2 py-0.5 text-xs text-[#9CA3AF] hover:text-white"
                        >
                          +
                        </button>
                      </div>
                      <span className="text-xs font-extrabold text-white font-mono w-16 text-right">
                        {formatCurrency(item.price * item.quantity)}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Desglose Neto e Impuestos */}
            <div className="border-t border-[#2D2D38] pt-3 space-y-1.5 text-xs text-[#9CA3AF]">
              <div className="flex justify-between">
                <span>Subtotal Neto:</span>
                <span className="font-mono text-white">{formatCurrency(subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span>IVA (19%):</span>
                <span className="font-mono text-white">{formatCurrency(taxAmount)}</span>
              </div>
            </div>
          </section>

          {/* ZONA 4: CONDICIONES DE PAGO Y TOTAL (PUNTO FOCAL PRINCIPAL) */}
          <section className="bg-gradient-to-b from-[#1E1E24] to-[#18181C] p-6 rounded-2xl border border-[#2D2D38] shadow-2xl space-y-6">
            
            {/* Selección de Método de Pago */}
            <div>
              <label className="block text-xs font-bold text-[#9CA3AF] uppercase tracking-wider mb-2">
                Método de Pago
              </label>
              <div className="grid grid-cols-3 gap-2">
                {(['TARJETA', 'EFECTIVO', 'TRANSFERENCIA'] as const).map((method) => (
                  <button
                    key={method}
                    onClick={() => setPaymentMethod(method)}
                    className={`py-2.5 px-2 rounded-xl text-xs font-bold border transition ${
                      paymentMethod === method
                        ? 'bg-[#10B981]/20 border-[#10B981] text-[#10B981]'
                        : 'bg-[#25252E] border-[#2D2D38] text-[#9CA3AF] hover:text-white'
                    }`}
                  >
                    {method === 'TARJETA' && '💳 Tarjeta'}
                    {method === 'EFECTIVO' && '💵 Efectivo'}
                    {method === 'TRANSFERENCIA' && '🏦 Transfer.'}
                  </button>
                ))}
              </div>
            </div>

            {/* FOCAL POINT: TOTAL DE LA FACTURA REFORZADO VISUALMENTE */}
            <div className="bg-[#121214] p-5 rounded-2xl border border-[#2D2D38] flex flex-col justify-center items-center text-center shadow-inner">
              <span className="text-xs font-bold text-[#9CA3AF] uppercase tracking-widest mb-1">
                TOTAL FACTURA
              </span>
              <span className="text-3xl md:text-4xl font-black text-[#10B981] font-mono tracking-tight">
                {formatCurrency(grandTotal)}
              </span>
            </div>

            {/* BOTÓN FOCAL PRINCIPAL: GENERAR FACTURA */}
            <button
              disabled={cart.length === 0}
              onClick={() => {
                alert(`Factura por ${formatCurrency(grandTotal)} generada con éxito.`);
                setCart([]);
              }}
              className="w-full bg-[#10B981] hover:bg-[#059669] disabled:opacity-40 disabled:hover:bg-[#10B981] text-white font-extrabold text-base py-4 rounded-xl shadow-xl shadow-emerald-950/60 transition duration-150 transform active:scale-[0.98] flex items-center justify-center gap-2"
            >
              <span>⚡</span> GENERAR FACTURA POS
            </button>

          </section>

        </div>

      </div>

    </div>
  );
};
