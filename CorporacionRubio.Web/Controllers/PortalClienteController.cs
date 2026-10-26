using CorporacionRubio.Business;
using CorporacionRubio.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,Cliente")]
    public class PortalClienteController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public PortalClienteController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            var usuarioId = ObtenerUsuarioId();
            var pedidos = _logisticaService.ObtenerPedidos()
                .Where(p => p.IdUsuario == usuarioId)
                .ToList();

            var model = new PortalClienteViewModel
            {
                NombreCliente = HttpContext.Session.GetString("UserName") ?? User?.Identity?.Name ?? "Cliente",
                Productos = _logisticaService.ObtenerInventario()
                    .Where(p => (p.StockActual ?? 0) > 0)
                    .ToList(),
                MisPedidos = pedidos
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SolicitarPedido(int[] idProductos, int[] cantidades, string metodoPago, string tipoComprobante, string? observacionPago)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(metodoPago))
                    throw new ArgumentException("Seleccione un método de pago o condición comercial para registrar la compra.");

                var idUsuario = ObtenerUsuarioId();
                var idPedido = _logisticaService.RegistrarSolicitudCliente(idUsuario, idProductos, cantidades, idUsuario, metodoPago, null, tipoComprobante, observacionPago);
                TempData["Mensaje"] = $"Compra #{idPedido} registrada. El sistema validó disponibilidad, generó el código de operación y envió la solicitud a Ventas para validar la condición de pago.";
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
