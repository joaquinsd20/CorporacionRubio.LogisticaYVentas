using CorporacionRubio.Business;
using CorporacionRubio.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,Ventas")]
    public class PanelVentasController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public PanelVentasController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? User?.Identity?.Name ?? "Asesor comercial";
            var model = new PanelVentasViewModel
            {
                Resumen = _logisticaService.ObtenerDashboard(),
                Pedidos = _logisticaService.ObtenerPedidosCompleto()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AprobarPedido(int idPedido)
        {
            try
            {
                _logisticaService.AprobarSolicitudPedido(idPedido, ObtenerUsuarioId());
                TempData["Mensaje"] = $"Compra #{idPedido} aprobada. La condición de pago fue validada, el stock quedó descontado y la orden fue derivada a logística.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RechazarPedido(int idPedido, string? motivo)
        {
            try
            {
                _logisticaService.RechazarSolicitudPedido(idPedido, ObtenerUsuarioId(), motivo);
                TempData["Mensaje"] = $"Compra #{idPedido} observada. La validación comercial quedó registrada en auditoría.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private int ObtenerUsuarioId()
        {
            return int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var idUsuario) ? idUsuario : 1;
        }
    }
}
