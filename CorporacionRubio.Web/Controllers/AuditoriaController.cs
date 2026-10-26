using CorporacionRubio.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador,JefeLogistica")]
    public class AuditoriaController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public AuditoriaController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")) && User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            var auditorias = _logisticaService.ObtenerAuditorias();
            return View(auditorias);
        }
    }
}
