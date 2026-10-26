using CorporacionRubio.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,Ventas,JefeLogistica")]
    public class PedidosController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public PedidosController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            if (!UsuarioEnSesion())
            {
                return RedirectToAction("Login", "Account");
            }

            var pedidos = _logisticaService.ObtenerPedidosCompleto();
            return View(pedidos);
        }

        [HttpGet]
        public IActionResult Detalle(int idPedido)
        {
            if (!UsuarioEnSesion())
            {
                return Unauthorized();
            }

            var detalle = _logisticaService.ObtenerDetallePedido(idPedido);
            return Json(detalle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarPedido(int idUsuarioCliente, int[] idProductos, int[] cantidades, string? metodoPago, string? referenciaPago, string? tipoComprobante, string? observacionPago)
        {
            if (!UsuarioEnSesion())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var idPedido = _logisticaService.RegistrarPedido(idUsuarioCliente, idProductos, cantidades, ObtenerUsuarioId(), metodoPago, referenciaPago, tipoComprobante, observacionPago);
                TempData["Mensaje"] = $"Venta #{idPedido} registrada correctamente. El pago quedó validado, el stock se descontó y se generó auditoría.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarEstado(int idPedido, string estado)
        {
            if (!UsuarioEnSesion())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _logisticaService.ActualizarEstadoPedido(idPedido, estado, ObtenerUsuarioId());
                TempData["Mensaje"] = "Estado del pedido actualizado correctamente.";
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
