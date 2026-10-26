using CorporacionRubio.Business;
using CorporacionRubio.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,JefeLogistica")]
    public class PanelLogisticoController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public PanelLogisticoController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? User?.Identity?.Name ?? "Usuario logístico";
            var model = new PanelLogisticoViewModel
            {
                Resumen = _logisticaService.ObtenerDashboard(),
                Inventario = _logisticaService.ObtenerInventarioCompleto(),
                Pedidos = _logisticaService.ObtenerPedidosCompleto()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEtapa(int idPedido, string etapa)
        {
            try
            {
                _logisticaService.ActualizarEstadoLogistico(idPedido, etapa, ObtenerUsuarioId());
                TempData["Mensaje"] = $"Pedido #{idPedido} actualizado a etapa: {etapa}.";
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
