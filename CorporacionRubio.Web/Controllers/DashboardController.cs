using CorporacionRubio.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DashboardController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public DashboardController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")) && User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? User?.Identity?.Name ?? "Usuario";
            ViewBag.UserRol = HttpContext.Session.GetString("UserRol");

            var resumen = _logisticaService.ObtenerDashboard();
            return View(resumen);
        }
    }
}
