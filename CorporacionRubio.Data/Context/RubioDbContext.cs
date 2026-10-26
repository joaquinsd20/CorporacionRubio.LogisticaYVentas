using CorporacionRubio.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CorporacionRubio.Data
{
    public class RubioDbContext : IdentityDbContext<ApplicationUser>
    {
        public RubioDbContext(DbContextOptions<RubioDbContext> options) : base(options) { }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Auditoria> Auditorias => Set<Auditoria>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<MovimientoInventario> MovimientosInventarios => Set<MovimientoInventario>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<DetallePedido> DetallesPedidos => Set<DetallePedido>();
        public DbSet<ComprobantePagoPedido> ComprobantesPagoPedidos => Set<ComprobantePagoPedido>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(e => e.IdRol);
                entity.Property(e => e.NombreRol).IsRequired().HasMaxLength(50).IsUnicode(false);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[email] IS NOT NULL");
                entity.Property(e => e.Contrasena).HasMaxLength(255).IsUnicode(false);
                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Auditoria>(entity =>
            {
                entity.HasKey(e => e.IdEvento);
                entity.Property(e => e.TablaAfectada).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.Accion).HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.FechaHora).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ValorAnterior).HasMaxLength(200).IsUnicode(false);
                entity.Property(e => e.ValorNuevo).HasMaxLength(200).IsUnicode(false);
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Auditorias)
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(e => e.IdCategoria);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
            });

            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(e => e.IdProveedor);
                entity.Property(e => e.NombreEmpresa).IsRequired().HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Ruc).HasMaxLength(15).IsUnicode(false);
                entity.HasIndex(e => e.Ruc).IsUnique().HasFilter("[ruc] IS NOT NULL");
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.IdProducto);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Descripcion).HasMaxLength(255).IsUnicode(false);
                entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10,2)");
                entity.Property(e => e.StockActual).HasDefaultValue(0);
                entity.HasOne(e => e.Categoria)
                    .WithMany(c => c.Productos)
                    .HasForeignKey(e => e.IdCategoria)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.Productos)
                    .HasForeignKey(e => e.IdProveedor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MovimientoInventario>(entity =>
            {
                entity.HasKey(e => e.IdMovimiento);
                entity.Property(e => e.TipoMovimiento).IsRequired().HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.FechaMovimiento).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Producto)
                    .WithMany(p => p.MovimientosInventario)
                    .HasForeignKey(e => e.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.MovimientosInventario)
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.IdPedido);
                entity.Property(e => e.FechaPedido).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Estado).HasMaxLength(50).IsUnicode(false);
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Pedidos)
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DetallePedido>(entity =>
            {
                entity.HasKey(e => e.IdDetalle);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.Pedido)
                    .WithMany(p => p.DetallesPedido)
                    .HasForeignKey(e => e.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Producto)
                    .WithMany(p => p.DetallesPedido)
                    .HasForeignKey(e => e.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ComprobantePagoPedido>(entity =>
            {
                entity.HasKey(e => e.IdPago);
                entity.Property(e => e.MetodoPago).IsRequired().HasMaxLength(40).IsUnicode(false);
                entity.Property(e => e.ReferenciaOperacion).IsRequired().HasMaxLength(80).IsUnicode(false);
                entity.Property(e => e.TipoComprobante).HasMaxLength(40).IsUnicode(false);
                entity.Property(e => e.EstadoPago).IsRequired().HasMaxLength(40).IsUnicode(false);
                entity.Property(e => e.Observacion).HasMaxLength(200).IsUnicode(false);
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Pedido)
                    .WithOne(p => p.PagoPedido)
                    .HasForeignKey<ComprobantePagoPedido>(e => e.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
