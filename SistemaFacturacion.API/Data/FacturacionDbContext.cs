using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.API.Data
{
    public class FacturacionDbContext : DbContext
    {
        public FacturacionDbContext(DbContextOptions<FacturacionDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Impuesto> Impuestos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetalleFacturas { get; set; }
        public DbSet<Cotizacion> Cotizaciones { get; set; }
        public DbSet<DetalleCotizacion> DetalleCotizaciones { get; set; }
        public DbSet<NotaCredito> NotasCredito { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CajaSesion> CajaSesiones { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }
        public DbSet<AbonoCliente> AbonosCliente { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Precisiones
            modelBuilder.Entity<Producto>().Property(p => p.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<Impuesto>().Property(i => i.Porcentaje).HasPrecision(5, 2);
            modelBuilder.Entity<Factura>().Property(f => f.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.DescuentoPorcentaje).HasPrecision(5, 2);
            modelBuilder.Entity<Factura>().Property(f => f.DescuentoMonto).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.ImpuestosTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleFactura>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleFactura>().Property(d => d.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleFactura>().Property(d => d.ImpuestoMonto).HasPrecision(18, 2);

            modelBuilder.Entity<Cotizacion>().Property(c => c.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Cotizacion>().Property(c => c.ImpuestosTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Cotizacion>().Property(c => c.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleCotizacion>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleCotizacion>().Property(d => d.Subtotal).HasPrecision(18, 2);

            modelBuilder.Entity<NotaCredito>().Property(n => n.MontoTotal).HasPrecision(18, 2);

            // Relaciones
            modelBuilder.Entity<Factura>()
                .HasOne(f => f.Cliente)
                .WithMany()
                .HasForeignKey(f => f.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleFactura>()
                .HasOne(d => d.Factura)
                .WithMany(f => f.Detalles)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cotizacion>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleCotizacion>()
                .HasOne(d => d.Cotizacion)
                .WithMany(c => c.Detalles)
                .HasForeignKey(d => d.CotizacionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotaCredito>()
                .HasOne(n => n.Factura)
                .WithMany()
                .HasForeignKey(n => n.FacturaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Producto)
                .WithMany()
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
