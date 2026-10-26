using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CorporacionRubio.Web.Models;
using CorporacionRubio.Business;

namespace CorporacionRubio.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly LogisticaService _logisticaService;

        public HomeController(LogisticaService logisticaService)
        {
            _logisticaService = logisticaService;
        }

        public IActionResult Index()
        {
            var productos = _logisticaService.ObtenerInventario()
                .Where(p => (p.StockActual ?? 0) > 0)
                .OrderBy(p => p.Categoria?.Nombre)
                .ThenBy(p => p.Nombre)
                .ToList();

            var model = new LandingCatalogViewModel
            {
                Productos = productos,
                Categorias = productos
                    .Where(p => p.Categoria != null)
                    .Select(p => p.Categoria!)
                    .GroupBy(c => c.IdCategoria)
                    .Select(g => g.First())
                    .OrderBy(c => c.Nombre)
                    .ToList(),
                StockTotal = productos.Sum(p => p.StockActual ?? 0),
                ValorCatalogo = productos.Sum(p => (p.PrecioUnitario ?? 0m) * (p.StockActual ?? 0))
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
