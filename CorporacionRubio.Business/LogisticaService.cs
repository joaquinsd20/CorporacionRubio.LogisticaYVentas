using CorporacionRubio.Business.Dto;
using CorporacionRubio.Data;
using CorporacionRubio.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CorporacionRubio.Business
{
    public class LogisticaService
    {
        private readonly RubioDbContext _context;

        public LogisticaService(RubioDbContext context)
        {
            _context = context;
        }

        public Usuario? ValidarUsuario(string email, string password)
        {
            var claveHash = ObtenerSha256(password);

            return _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefault(u => u.Email == email &&
                                     (u.Contrasena == claveHash || u.Contrasena == password));
        }

        public DashboardResumenDto ObtenerDashboard()
        {
            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsNoTracking()
                .ToList();

            return new DashboardResumenDto
            {
                TotalProductos = productos.Count,
                TotalPedidos = _context.Pedidos.Count(),
                TotalAuditorias = _context.Auditorias.Count(),
                TotalCategorias = _context.Categorias.Count(),
                TotalProveedores = _context.Proveedores.Count(),
                TotalMovimientos = _context.MovimientosInventarios.Count(),
                StockTotal = productos.Sum(p => p.StockActual ?? 0),
                ValorInventario = productos.Sum(p => (p.PrecioUnitario ?? 0m) * (p.StockActual ?? 0)),
                TotalVentas = _context.DetallesPedidos
                    .Where(d => d.Pedido != null && new[] { "Aprobado por ventas", "Preparación logística", "Despachado", "Entregado" }.Contains(d.Pedido.Estado ?? ""))
                    .Sum(d => d.Subtotal ?? 0m),
                ProductosStockCritico = productos.Count(p => (p.StockActual ?? 0) <= 10),
                ProductosCriticos = productos
                    .Where(p => (p.StockActual ?? 0) <= 10)
                    .OrderBy(p => p.StockActual ?? 0)
                    .Take(6)
                    .ToList(),
                PedidosRecientes = _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.DetallesPedido)
                    .AsNoTracking()
                    .OrderByDescending(p => p.FechaPedido)
                    .Take(5)
                    .ToList(),
                MovimientosRecientes = _context.MovimientosInventarios
                    .Include(m => m.Producto)
                    .Include(m => m.Usuario)
                    .AsNoTracking()
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(5)
                    .ToList()
            };
        }

        public InventarioIndexDto ObtenerInventarioCompleto()
        {
            return new InventarioIndexDto
            {
                Productos = _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Proveedor)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ToList(),
                Categorias = ObtenerCategorias(),
                Proveedores = ObtenerProveedores(),
                MovimientosRecientes = _context.MovimientosInventarios
                    .Include(m => m.Producto)
                    .Include(m => m.Usuario)
                    .AsNoTracking()
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(20)
                    .ToList()
            };
        }

        public List<Producto> ObtenerInventario()
        {
            return _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .ToList();
        }

        public List<Categoria> ObtenerCategorias()
        {
            return _context.Categorias
                .Include(c => c.Productos)
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToList();
        }

        public List<Proveedor> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.Productos)
                .AsNoTracking()
                .OrderBy(p => p.NombreEmpresa)
                .ToList();
        }

        public List<Usuario> ObtenerUsuarios()
        {
            return _context.Usuarios
                .Include(u => u.Rol)
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .ToList();
        }

        public bool RegistrarNuevoProducto(string nombre, string? descripcion, decimal precio, int stock, int idCategoria, int idProveedor, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del producto es obligatorio.");
            if (precio <= 0)
                throw new ArgumentException("El precio debe ser mayor a cero.");
            if (stock < 0)
                throw new ArgumentException("El stock inicial no puede ser negativo.");
            if (!_context.Categorias.Any(c => c.IdCategoria == idCategoria))
                throw new InvalidOperationException("Seleccione una categoría válida.");
            if (!_context.Proveedores.Any(p => p.IdProveedor == idProveedor))
                throw new InvalidOperationException("Seleccione un proveedor válido.");

            using var transaccion = _context.Database.BeginTransaction();

            var nuevoProducto = new Producto
            {
                Nombre = nombre.Trim(),
                Descripcion = descripcion?.Trim(),
                PrecioUnitario = precio,
                StockActual = stock,
                IdCategoria = idCategoria,
                IdProveedor = idProveedor
            };

            _context.Productos.Add(nuevoProducto);
            _context.SaveChanges();

            if (stock > 0)
            {
                _context.MovimientosInventarios.Add(new MovimientoInventario
                {
                    IdProducto = nuevoProducto.IdProducto,
                    TipoMovimiento = "INGRESO",
                    Cantidad = stock,
                    FechaMovimiento = DateTime.Now,
                    IdUsuario = idUsuario
                });
            }

            RegistrarAuditoriaInterna("Producto", "INSERT", idUsuario, null,
                $"Producto #{nuevoProducto.IdProducto}: {nuevoProducto.Nombre}; stock inicial {stock}");

            _context.SaveChanges();
            transaccion.Commit();
            return true;
        }

        public void ActualizarProducto(int idProducto, string nombre, string? descripcion, decimal precio, int idCategoria, int idProveedor, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del producto es obligatorio.");
            if (precio <= 0)
                throw new ArgumentException("El precio debe ser mayor a cero.");

            var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            if (producto == null)
                throw new InvalidOperationException("No se encontró el producto seleccionado.");
            if (!_context.Categorias.Any(c => c.IdCategoria == idCategoria))
                throw new InvalidOperationException("Seleccione una categoría válida.");
            if (!_context.Proveedores.Any(p => p.IdProveedor == idProveedor))
                throw new InvalidOperationException("Seleccione un proveedor válido.");

            var anterior = $"{producto.Nombre}; S/ {producto.PrecioUnitario:F2}; categoría {producto.IdCategoria}; proveedor {producto.IdProveedor}";

            producto.Nombre = nombre.Trim();
            producto.Descripcion = descripcion?.Trim();
            producto.PrecioUnitario = precio;
            producto.IdCategoria = idCategoria;
            producto.IdProveedor = idProveedor;

            RegistrarAuditoriaInterna("Producto", "UPDATE", idUsuario, anterior,
                $"{producto.Nombre}; S/ {producto.PrecioUnitario:F2}; categoría {producto.IdCategoria}; proveedor {producto.IdProveedor}");

            _context.SaveChanges();
        }

        public void EliminarProducto(int idProducto, int idUsuario)
        {
            using var transaccion = _context.Database.BeginTransaction();

            var producto = _context.Productos
                .Include(p => p.MovimientosInventario)
                .FirstOrDefault(p => p.IdProducto == idProducto);

            if (producto == null)
                throw new InvalidOperationException("No se encontró el producto seleccionado.");

            if (_context.DetallesPedidos.Any(d => d.IdProducto == idProducto))
                throw new InvalidOperationException("No se puede eliminar porque el producto ya está asociado a pedidos. Manténgalo como histórico y gestione su stock con una salida.");

            var movimientos = _context.MovimientosInventarios.Where(m => m.IdProducto == idProducto).ToList();
            _context.MovimientosInventarios.RemoveRange(movimientos);

            RegistrarAuditoriaInterna("Producto", "DELETE", idUsuario,
                $"Producto #{producto.IdProducto}: {producto.Nombre}",
                "Producto eliminado junto con sus movimientos sin pedido asociado");

            _context.Productos.Remove(producto);
            _context.SaveChanges();
            transaccion.Commit();
        }

        public void RegistrarMovimientoInventario(int idProducto, string tipoMovimiento, int cantidad, int idUsuario)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad del movimiento debe ser mayor a cero.");

            tipoMovimiento = (tipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();
            if (tipoMovimiento != "INGRESO" && tipoMovimiento != "SALIDA")
                throw new ArgumentException("El tipo de movimiento solo puede ser INGRESO o SALIDA.");

            using var transaccion = _context.Database.BeginTransaction();

            var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            if (producto == null)
                throw new InvalidOperationException("No se encontró el producto seleccionado.");

            var stockAnterior = producto.StockActual ?? 0;
            var stockNuevo = tipoMovimiento == "INGRESO" ? stockAnterior + cantidad : stockAnterior - cantidad;

            if (stockNuevo < 0)
                throw new InvalidOperationException($"No hay stock suficiente. Stock actual: {stockAnterior} unidades.");

            producto.StockActual = stockNuevo;

            _context.MovimientosInventarios.Add(new MovimientoInventario
            {
                IdProducto = idProducto,
                TipoMovimiento = tipoMovimiento,
                Cantidad = cantidad,
                FechaMovimiento = DateTime.Now,
                IdUsuario = idUsuario
            });

            RegistrarAuditoriaInterna("Movimiento_Inventario", "INSERT", idUsuario,
                $"Producto #{idProducto}: stock {stockAnterior}",
                $"{tipoMovimiento} de {cantidad}; stock nuevo {stockNuevo}");

            RegistrarAuditoriaInterna("Producto", "UPDATE", idUsuario,
                $"Stock anterior: {stockAnterior}",
                $"Stock nuevo: {stockNuevo}");

            _context.SaveChanges();
            transaccion.Commit();
        }

        public void RegistrarCategoria(string nombre, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la categoría es obligatorio.");

            nombre = nombre.Trim();
            if (_context.Categorias.Any(c => c.Nombre == nombre))
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");

            var categoria = new Categoria { Nombre = nombre };
            _context.Categorias.Add(categoria);
            RegistrarAuditoriaInterna("Categoria", "INSERT", idUsuario, null, $"Categoría: {nombre}");
            _context.SaveChanges();
        }

        public void ActualizarCategoria(int idCategoria, string nombre, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la categoría es obligatorio.");

            var categoria = _context.Categorias.FirstOrDefault(c => c.IdCategoria == idCategoria);
            if (categoria == null)
                throw new InvalidOperationException("No se encontró la categoría seleccionada.");

            var anterior = categoria.Nombre;
            categoria.Nombre = nombre.Trim();
            RegistrarAuditoriaInterna("Categoria", "UPDATE", idUsuario, anterior, categoria.Nombre);
            _context.SaveChanges();
        }

        public void EliminarCategoria(int idCategoria, int idUsuario)
        {
            var categoria = _context.Categorias.FirstOrDefault(c => c.IdCategoria == idCategoria);
            if (categoria == null)
                throw new InvalidOperationException("No se encontró la categoría seleccionada.");
            if (_context.Productos.Any(p => p.IdCategoria == idCategoria))
                throw new InvalidOperationException("No se puede eliminar porque tiene productos asociados.");

            RegistrarAuditoriaInterna("Categoria", "DELETE", idUsuario, categoria.Nombre, "Categoría eliminada");
            _context.Categorias.Remove(categoria);
            _context.SaveChanges();
        }

        public void RegistrarProveedor(string nombreEmpresa, string? ruc, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
                throw new ArgumentException("El nombre del proveedor es obligatorio.");

            nombreEmpresa = nombreEmpresa.Trim();
            ruc = string.IsNullOrWhiteSpace(ruc) ? null : ruc.Trim();

            if (ruc != null && _context.Proveedores.Any(p => p.Ruc == ruc))
                throw new InvalidOperationException("Ya existe un proveedor con ese RUC.");

            var proveedor = new Proveedor { NombreEmpresa = nombreEmpresa, Ruc = ruc };
            _context.Proveedores.Add(proveedor);
            RegistrarAuditoriaInterna("Proveedor", "INSERT", idUsuario, null, $"Proveedor: {nombreEmpresa}; RUC: {ruc ?? "Sin RUC"}");
            _context.SaveChanges();
        }

        public void ActualizarProveedor(int idProveedor, string nombreEmpresa, string? ruc, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
                throw new ArgumentException("El nombre del proveedor es obligatorio.");

            var proveedor = _context.Proveedores.FirstOrDefault(p => p.IdProveedor == idProveedor);
            if (proveedor == null)
                throw new InvalidOperationException("No se encontró el proveedor seleccionado.");

            ruc = string.IsNullOrWhiteSpace(ruc) ? null : ruc.Trim();
            if (ruc != null && _context.Proveedores.Any(p => p.Ruc == ruc && p.IdProveedor != idProveedor))
                throw new InvalidOperationException("Ya existe otro proveedor con ese RUC.");

            var anterior = $"{proveedor.NombreEmpresa}; RUC: {proveedor.Ruc ?? "Sin RUC"}";
            proveedor.NombreEmpresa = nombreEmpresa.Trim();
            proveedor.Ruc = ruc;

            RegistrarAuditoriaInterna("Proveedor", "UPDATE", idUsuario, anterior,
                $"{proveedor.NombreEmpresa}; RUC: {proveedor.Ruc ?? "Sin RUC"}");
            _context.SaveChanges();
        }

        public void EliminarProveedor(int idProveedor, int idUsuario)
        {
            var proveedor = _context.Proveedores.FirstOrDefault(p => p.IdProveedor == idProveedor);
            if (proveedor == null)
                throw new InvalidOperationException("No se encontró el proveedor seleccionado.");
            if (_context.Productos.Any(p => p.IdProveedor == idProveedor))
                throw new InvalidOperationException("No se puede eliminar porque tiene productos asociados.");

            RegistrarAuditoriaInterna("Proveedor", "DELETE", idUsuario, proveedor.NombreEmpresa, "Proveedor eliminado");
            _context.Proveedores.Remove(proveedor);
            _context.SaveChanges();
        }

        public List<Pedido> ObtenerPedidos()
        {
            return _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.PagoPedido)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPedido)
                .ToList();
        }

        public PedidosIndexDto ObtenerPedidosCompleto()
        {
            return new PedidosIndexDto
            {
                Pedidos = ObtenerPedidos(),
                Productos = _context.Productos
                    .Include(p => p.Categoria)
                    .AsNoTracking()
                    .Where(p => (p.StockActual ?? 0) > 0)
                    .OrderBy(p => p.Nombre)
                    .ToList(),
                Usuarios = ObtenerUsuarios()
            };
        }

        public List<PedidoDetalleDto> ObtenerDetallePedido(int idPedido)
        {
            return _context.DetallesPedidos
                .Include(d => d.Producto)
                .AsNoTracking()
                .Where(d => d.IdPedido == idPedido)
                .OrderBy(d => d.IdDetalle)
                .Select(d => new PedidoDetalleDto
                {
                    IdDetalle = d.IdDetalle,
                    Producto = d.Producto != null ? d.Producto.Nombre : $"Producto #{d.IdProducto}",
                    Cantidad = d.Cantidad,
                    Subtotal = d.Subtotal ?? 0m
                })
                .ToList();
        }

        public int RegistrarSolicitudCliente(int idUsuarioCliente, int[] idProductos, int[] cantidades, int idUsuarioOperador, string? metodoPago = null, string? referenciaPago = null, string? tipoComprobante = null, string? observacionPago = null)
        {
            if (!_context.Usuarios.Any(u => u.IdUsuario == idUsuarioCliente))
                throw new InvalidOperationException("No se encontró una cuenta de cliente válida para registrar la solicitud.");

            if (idProductos == null || cantidades == null || idProductos.Length == 0 || cantidades.Length == 0)
                throw new ArgumentException("Debe seleccionar al menos un producto del catálogo corporativo.");

            if (idProductos.Length != cantidades.Length)
                throw new ArgumentException("Los productos y cantidades ingresadas no coinciden.");

            metodoPago = string.IsNullOrWhiteSpace(metodoPago) ? null : metodoPago.Trim();
            if (metodoPago == null)
                throw new ArgumentException("Seleccione el método de pago o la condición comercial antes de registrar la compra.");

            var items = idProductos
                .Zip(cantidades, (productoId, cantidad) => new { productoId, cantidad })
                .Where(i => i.productoId > 0 && i.cantidad > 0)
                .GroupBy(i => i.productoId)
                .Select(g => new { productoId = g.Key, cantidad = g.Sum(x => x.cantidad) })
                .ToList();

            if (!items.Any())
                throw new ArgumentException("Debe indicar cantidades mayores a cero para generar una solicitud.");

            using var transaccion = _context.Database.BeginTransaction();

            var pedido = new Pedido
            {
                FechaPedido = DateTime.Now,
                IdUsuario = idUsuarioCliente,
                Estado = ObtenerEstadoInicialSolicitud(metodoPago)
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            decimal totalPedido = 0m;

            foreach (var item in items)
            {
                var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == item.productoId);
                if (producto == null)
                    throw new InvalidOperationException($"No se encontró el producto #{item.productoId}.");

                var stockActual = producto.StockActual ?? 0;
                if (stockActual <= 0)
                    throw new InvalidOperationException($"{producto.Nombre} no tiene stock disponible para solicitud.");

                if (stockActual < item.cantidad)
                    throw new InvalidOperationException($"Stock insuficiente para {producto.Nombre}. Disponible: {stockActual}; solicitado: {item.cantidad}. Ajuste la cantidad antes de continuar.");

                var precio = producto.PrecioUnitario ?? 0m;
                var subtotal = precio * item.cantidad;
                totalPedido += subtotal;

                _context.DetallesPedidos.Add(new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = item.cantidad,
                    Subtotal = subtotal
                });
            }

            var codigoOperacion = string.IsNullOrWhiteSpace(referenciaPago)
                ? GenerarCodigoOperacion(pedido.IdPedido, metodoPago)
                : referenciaPago.Trim();

            _context.ComprobantesPagoPedidos.Add(new ComprobantePagoPedido
            {
                IdPedido = pedido.IdPedido,
                MetodoPago = metodoPago,
                ReferenciaOperacion = codigoOperacion,
                TipoComprobante = string.IsNullOrWhiteSpace(tipoComprobante) ? "Boleta" : tipoComprobante.Trim(),
                EstadoPago = ObtenerEstadoPagoInicial(metodoPago),
                Observacion = string.IsNullOrWhiteSpace(observacionPago) ? null : observacionPago.Trim(),
                FechaRegistro = DateTime.Now
            });

            RegistrarAuditoriaInterna("Pedido", "INSERT", idUsuarioOperador, null,
                $"Compra web #{pedido.IdPedido}; cliente #{idUsuarioCliente}; importe S/ {totalPedido:F2}; disponibilidad validada; método {metodoPago}; código {codigoOperacion}; estado {pedido.Estado}");

            _context.SaveChanges();
            transaccion.Commit();
            return pedido.IdPedido;
        }

        public int RegistrarPedido(int idUsuarioCliente, int[] idProductos, int[] cantidades, int idUsuarioOperador, string? metodoPago = null, string? referenciaPago = null, string? tipoComprobante = null, string? observacionPago = null)
        {
            if (!_context.Usuarios.Any(u => u.IdUsuario == idUsuarioCliente))
                throw new InvalidOperationException("Seleccione un cliente o usuario válido.");

            if (idProductos == null || cantidades == null || idProductos.Length == 0 || cantidades.Length == 0)
                throw new ArgumentException("Debe agregar al menos un producto al pedido.");

            if (idProductos.Length != cantidades.Length)
                throw new ArgumentException("Los productos y cantidades del pedido no coinciden.");

            using var transaccion = _context.Database.BeginTransaction();

            var pedido = new Pedido
            {
                FechaPedido = DateTime.Now,
                IdUsuario = idUsuarioCliente,
                Estado = "Aprobado por ventas"
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            var items = idProductos
                .Zip(cantidades, (productoId, cantidad) => new { productoId, cantidad })
                .Where(i => i.productoId > 0 && i.cantidad > 0)
                .GroupBy(i => i.productoId)
                .Select(g => new { productoId = g.Key, cantidad = g.Sum(x => x.cantidad) })
                .ToList();

            if (!items.Any())
                throw new ArgumentException("Debe indicar cantidades mayores a cero.");

            metodoPago = string.IsNullOrWhiteSpace(metodoPago) ? null : metodoPago.Trim();
            var pagoInformado = !string.IsNullOrWhiteSpace(metodoPago);

            decimal totalPedido = 0m;

            foreach (var item in items)
            {
                var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == item.productoId);
                if (producto == null)
                    throw new InvalidOperationException($"No se encontró el producto #{item.productoId}.");

                var stockAnterior = producto.StockActual ?? 0;
                if (stockAnterior < item.cantidad)
                    throw new InvalidOperationException($"Stock insuficiente para {producto.Nombre}. Stock actual: {stockAnterior} unidades.");

                var precio = producto.PrecioUnitario ?? 0m;
                var subtotal = precio * item.cantidad;
                totalPedido += subtotal;
                producto.StockActual = stockAnterior - item.cantidad;

                _context.DetallesPedidos.Add(new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = item.cantidad,
                    Subtotal = subtotal
                });

                _context.MovimientosInventarios.Add(new MovimientoInventario
                {
                    IdProducto = producto.IdProducto,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.cantidad,
                    FechaMovimiento = DateTime.Now,
                    IdUsuario = idUsuarioOperador
                });

                RegistrarAuditoriaInterna("Producto", "UPDATE", idUsuarioOperador,
                    $"{producto.Nombre}: stock {stockAnterior}",
                    $"Pedido #{pedido.IdPedido}: salida {item.cantidad}; stock nuevo {producto.StockActual}");
            }

            if (pagoInformado)
            {
                var codigoOperacion = string.IsNullOrWhiteSpace(referenciaPago)
                    ? GenerarCodigoOperacion(pedido.IdPedido, metodoPago!)
                    : referenciaPago.Trim();

                _context.ComprobantesPagoPedidos.Add(new ComprobantePagoPedido
                {
                    IdPedido = pedido.IdPedido,
                    MetodoPago = metodoPago!,
                    ReferenciaOperacion = codigoOperacion,
                    TipoComprobante = string.IsNullOrWhiteSpace(tipoComprobante) ? "Boleta" : tipoComprobante.Trim(),
                    EstadoPago = ObtenerEstadoPagoValidado(metodoPago!),
                    Observacion = string.IsNullOrWhiteSpace(observacionPago) ? "Condición de pago validada al registrar venta interna" : observacionPago.Trim(),
                    FechaRegistro = DateTime.Now,
                    FechaValidacion = DateTime.Now,
                    IdUsuarioValidacion = idUsuarioOperador
                });
            }

            RegistrarAuditoriaInterna("Pedido", "INSERT", idUsuarioOperador, null,
                $"Pedido interno #{pedido.IdPedido}; cliente #{idUsuarioCliente}; total S/ {totalPedido:F2}; estado Aprobado por ventas; pago {(pagoInformado ? "validado" : "no registrado")}");

            _context.SaveChanges();
            transaccion.Commit();
            return pedido.IdPedido;
        }

        public void AprobarSolicitudPedido(int idPedido, int idUsuarioOperador)
        {
            using var transaccion = _context.Database.BeginTransaction();

            var pedido = _context.Pedidos
                .Include(p => p.PagoPedido)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefault(p => p.IdPedido == idPedido);

            if (pedido == null)
                throw new InvalidOperationException("No se encontró la solicitud seleccionada.");

            var estadoAnterior = pedido.Estado ?? "Sin estado";
            var estadosAprobables = new[]
            {
                "Pago por validar",
                "Solicitud recibida",
                "En revisión comercial",
                "Pendiente",
                "Disponibilidad validada - pago por validar",
                "Disponibilidad validada - condición por aprobar",
                "Disponibilidad validada - crédito por aprobar"
            };

            if (!estadosAprobables.Contains(estadoAnterior))
                throw new InvalidOperationException($"La solicitud no puede aprobarse porque se encuentra en estado: {estadoAnterior}.");

            if (pedido.DetallesPedido == null || !pedido.DetallesPedido.Any())
                throw new InvalidOperationException("La solicitud no tiene productos asociados.");

            if (pedido.PagoPedido == null || string.IsNullOrWhiteSpace(pedido.PagoPedido.ReferenciaOperacion))
                throw new InvalidOperationException("No se puede aprobar la compra porque no existe código de operación generado por el sistema.");

            foreach (var detalle in pedido.DetallesPedido)
            {
                var producto = detalle.Producto ?? _context.Productos.FirstOrDefault(p => p.IdProducto == detalle.IdProducto);
                if (producto == null)
                    throw new InvalidOperationException($"No se encontró el producto #{detalle.IdProducto}.");

                var stockAnterior = producto.StockActual ?? 0;
                if (stockAnterior < detalle.Cantidad)
                    throw new InvalidOperationException($"No hay stock suficiente para aprobar {producto.Nombre}. Stock actual: {stockAnterior}; solicitado: {detalle.Cantidad}.");

                producto.StockActual = stockAnterior - detalle.Cantidad;

                _context.MovimientosInventarios.Add(new MovimientoInventario
                {
                    IdProducto = producto.IdProducto,
                    TipoMovimiento = "SALIDA",
                    Cantidad = detalle.Cantidad,
                    FechaMovimiento = DateTime.Now,
                    IdUsuario = idUsuarioOperador
                });

                RegistrarAuditoriaInterna("Producto", "UPDATE", idUsuarioOperador,
                    $"{producto.Nombre}: stock {stockAnterior}",
                    $"Aprobación comercial de pedido #{pedido.IdPedido}: salida {detalle.Cantidad}; stock nuevo {producto.StockActual}");
            }

            pedido.PagoPedido.EstadoPago = ObtenerEstadoPagoValidado(pedido.PagoPedido.MetodoPago);
            pedido.PagoPedido.FechaValidacion = DateTime.Now;
            pedido.PagoPedido.IdUsuarioValidacion = idUsuarioOperador;

            pedido.Estado = "Aprobado por ventas";
            RegistrarAuditoriaInterna("Pedido", "UPDATE", idUsuarioOperador,
                $"Pedido #{idPedido}: {estadoAnterior}",
                $"Pedido #{idPedido}: Aprobado por ventas; código {pedido.PagoPedido.ReferenciaOperacion}; stock descontado y derivado a logística");

            _context.SaveChanges();
            transaccion.Commit();
        }

        public void RechazarSolicitudPedido(int idPedido, int idUsuarioOperador, string? motivo = null)
        {
            var pedido = _context.Pedidos
                .Include(p => p.PagoPedido)
                .FirstOrDefault(p => p.IdPedido == idPedido);
            if (pedido == null)
                throw new InvalidOperationException("No se encontró la solicitud seleccionada.");

            var estadoAnterior = pedido.Estado ?? "Sin estado";
            if (estadoAnterior == "Entregado" || estadoAnterior == "Despachado")
                throw new InvalidOperationException("No se puede rechazar una solicitud que ya fue despachada o entregada.");

            pedido.Estado = "Rechazado por ventas";
            if (pedido.PagoPedido != null)
            {
                pedido.PagoPedido.EstadoPago = "Observado";
                pedido.PagoPedido.Observacion = string.IsNullOrWhiteSpace(motivo) ? "Compra observada por el área comercial" : motivo.Trim();
            }
            RegistrarAuditoriaInterna("Pedido", "UPDATE", idUsuarioOperador,
                $"Pedido #{idPedido}: {estadoAnterior}",
                $"Pedido #{idPedido}: Rechazado por ventas. Motivo: {(string.IsNullOrWhiteSpace(motivo) ? "Sin observación" : motivo.Trim())}");

            _context.SaveChanges();
        }

        public void ActualizarEstadoLogistico(int idPedido, string nuevoEstado, int idUsuarioOperador)
        {
            var estadosPermitidos = new[] { "Preparación logística", "Despachado", "Entregado" };
            if (string.IsNullOrWhiteSpace(nuevoEstado) || !estadosPermitidos.Contains(nuevoEstado.Trim()))
                throw new ArgumentException("Seleccione una etapa logística válida.");

            nuevoEstado = nuevoEstado.Trim();
            var pedido = _context.Pedidos.FirstOrDefault(p => p.IdPedido == idPedido);
            if (pedido == null)
                throw new InvalidOperationException("No se encontró el pedido seleccionado.");

            var estadoAnterior = pedido.Estado ?? "Sin estado";
            var permitido = nuevoEstado switch
            {
                "Preparación logística" => estadoAnterior == "Aprobado por ventas",
                "Despachado" => estadoAnterior == "Preparación logística",
                "Entregado" => estadoAnterior == "Despachado",
                _ => false
            };

            if (!permitido)
                throw new InvalidOperationException($"No se puede cambiar de '{estadoAnterior}' a '{nuevoEstado}'. Respete el flujo: Aprobado por ventas → Preparación logística → Despachado → Entregado.");

            pedido.Estado = nuevoEstado;
            RegistrarAuditoriaInterna("Pedido", "UPDATE", idUsuarioOperador,
                $"Pedido #{idPedido}: {estadoAnterior}",
                $"Pedido #{idPedido}: {nuevoEstado}");

            _context.SaveChanges();
        }

        public void ActualizarEstadoPedido(int idPedido, string nuevoEstado, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(nuevoEstado))
                throw new ArgumentException("Debe seleccionar un estado válido.");

            var estadosPermitidos = new[] { "Solicitud recibida", "En revisión comercial", "Disponibilidad validada - pago por validar", "Disponibilidad validada - condición por aprobar", "Disponibilidad validada - crédito por aprobar", "Aprobado por ventas", "Preparación logística", "Despachado", "Entregado", "Rechazado por ventas", "Pendiente", "Procesado", "Validado", "En Despacho", "Cancelado" };
            nuevoEstado = nuevoEstado.Trim();
            if (!estadosPermitidos.Contains(nuevoEstado))
                throw new ArgumentException("El estado seleccionado no es válido.");

            var pedido = _context.Pedidos.FirstOrDefault(p => p.IdPedido == idPedido);
            if (pedido == null)
                throw new InvalidOperationException("No se encontró el pedido seleccionado.");

            var estadoAnterior = pedido.Estado ?? "Sin estado";
            pedido.Estado = nuevoEstado;

            RegistrarAuditoriaInterna("Pedido", "UPDATE", idUsuario,
                $"Pedido #{idPedido}: {estadoAnterior}",
                $"Pedido #{idPedido}: {pedido.Estado}");

            _context.SaveChanges();
        }

        public List<Auditoria> ObtenerAuditorias()
        {
            return _context.Auditorias
                .Include(a => a.Usuario)
                .AsNoTracking()
                .OrderByDescending(a => a.FechaHora)
                .Take(200)
                .ToList();
        }

        private static string ObtenerEstadoInicialSolicitud(string metodoPago)
        {
            if (metodoPago.Contains("contra", StringComparison.OrdinalIgnoreCase))
                return "Disponibilidad validada - condición por aprobar";

            if (metodoPago.Contains("crédito", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("credito", StringComparison.OrdinalIgnoreCase))
                return "Disponibilidad validada - crédito por aprobar";

            return "Disponibilidad validada - pago por validar";
        }

        private static string ObtenerEstadoPagoInicial(string metodoPago)
        {
            if (metodoPago.Contains("contra", StringComparison.OrdinalIgnoreCase))
                return "Pendiente contra entrega";

            if (metodoPago.Contains("crédito", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("credito", StringComparison.OrdinalIgnoreCase))
                return "Crédito por evaluar";

            return "Pendiente de validación";
        }

        private static string ObtenerEstadoPagoValidado(string metodoPago)
        {
            if (metodoPago.Contains("contra", StringComparison.OrdinalIgnoreCase))
                return "Autorizado contra entrega";

            if (metodoPago.Contains("crédito", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("credito", StringComparison.OrdinalIgnoreCase))
                return "Crédito autorizado";

            return "Validado";
        }

        private static string GenerarCodigoOperacion(int idPedido, string metodoPago)
        {
            var prefijo = "CR-PAG";
            if (metodoPago.Contains("contra", StringComparison.OrdinalIgnoreCase))
                prefijo = "CR-CDE";
            else if (metodoPago.Contains("crédito", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("credito", StringComparison.OrdinalIgnoreCase))
                prefijo = "CR-CRE";
            else if (metodoPago.Contains("yape", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("plin", StringComparison.OrdinalIgnoreCase))
                prefijo = "CR-APP";
            else if (metodoPago.Contains("transfer", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("depósito", StringComparison.OrdinalIgnoreCase) || metodoPago.Contains("deposito", StringComparison.OrdinalIgnoreCase))
                prefijo = "CR-BNK";

            return $"{prefijo}-{DateTime.Now:yyyyMMdd}-{idPedido:D5}";
        }

        private void RegistrarAuditoriaInterna(string tabla, string accion, int idUsuario, string? valorAnterior, string? valorNuevo)
        {
            _context.Auditorias.Add(new Auditoria
            {
                TablaAfectada = tabla,
                Accion = accion,
                FechaHora = DateTime.Now,
                IdUsuario = idUsuario,
                ValorAnterior = valorAnterior,
                ValorNuevo = valorNuevo
            });
        }

        private static string ObtenerSha256(string texto)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
