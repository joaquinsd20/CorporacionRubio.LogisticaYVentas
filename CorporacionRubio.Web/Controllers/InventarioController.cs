using CorporacionRubio.Business;
using CorporacionRubio.Business.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,JefeLogistica")]
    public class InventarioController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public InventarioController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            if (!UsuarioEnSesion())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var inventario = _logisticaService.ObtenerInventarioCompleto();
                return View(inventario);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error operativo al recuperar el inventario: " + ex.Message;
                return View(new InventarioIndexDto());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarProducto(string nombre, string? descripcion, decimal precio, int stock, int idCategoria, int idProveedor)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.RegistrarNuevoProducto(nombre, descripcion, precio, stock, idCategoria, idProveedor, ObtenerUsuarioId());
                TempData["Mensaje"] = "Producto registrado correctamente. Se generó su movimiento inicial y auditoría.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProducto(int idProducto, string nombre, string? descripcion, decimal precio, int idCategoria, int idProveedor)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.ActualizarProducto(idProducto, nombre, descripcion, precio, idCategoria, idProveedor, ObtenerUsuarioId());
                TempData["Mensaje"] = "Producto actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int idProducto)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.EliminarProducto(idProducto, ObtenerUsuarioId());
                TempData["Mensaje"] = "Producto eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarMovimiento(int idProducto, string tipoMovimiento, int cantidad)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.RegistrarMovimientoInventario(idProducto, tipoMovimiento, cantidad, ObtenerUsuarioId());
                TempData["Mensaje"] = "Movimiento de inventario registrado y stock actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarCategoria(string nombre)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.RegistrarCategoria(nombre, ObtenerUsuarioId());
                TempData["Mensaje"] = "Categoría registrada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarCategoria(int idCategoria, string nombre)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.ActualizarCategoria(idCategoria, nombre, ObtenerUsuarioId());
                TempData["Mensaje"] = "Categoría actualizada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarCategoria(int idCategoria)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.EliminarCategoria(idCategoria, ObtenerUsuarioId());
                TempData["Mensaje"] = "Categoría eliminada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarProveedor(string nombreEmpresa, string? ruc)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.RegistrarProveedor(nombreEmpresa, ruc, ObtenerUsuarioId());
                TempData["Mensaje"] = "Proveedor registrado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProveedor(int idProveedor, string nombreEmpresa, string? ruc)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.ActualizarProveedor(idProveedor, nombreEmpresa, ruc, ObtenerUsuarioId());
                TempData["Mensaje"] = "Proveedor actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProveedor(int idProveedor)
        {
            if (!UsuarioEnSesion()) return RedirectToAction("Login", "Account");

            try
            {
                _logisticaService.EliminarProveedor(idProveedor, ObtenerUsuarioId());
                TempData["Mensaje"] = "Proveedor eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private bool UsuarioEnSesion()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")) || User?.Identity?.IsAuthenticated == true;
        }

        private int ObtenerUsuarioId()
        {
            return int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var idUsuario) ? idUsuario : 1;
        }
    }
}
