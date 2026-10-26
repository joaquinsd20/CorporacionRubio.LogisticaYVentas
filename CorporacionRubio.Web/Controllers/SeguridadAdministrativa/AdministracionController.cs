using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorporacionRubio.Data;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministracionController : Controller
    {
        private readonly RubioDbContext _context;

        public AdministracionController(RubioDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Productos = await _context.Productos.CountAsync();
            ViewBag.Pedidos = await _context.Pedidos.CountAsync();
            ViewBag.Movimientos = await _context.MovimientosInventarios.CountAsync();
            ViewBag.Auditorias = await _context.Auditorias.CountAsync();
            ViewBag.StockCritico = await _context.Productos.CountAsync(p => (p.StockActual ?? 0) <= 10);
            ViewBag.PedidosPendientes = await _context.Pedidos.CountAsync(p => p.Estado != null && (p.Estado.Contains("Solicitud") || p.Estado.Contains("Pendiente") || p.Estado.Contains("Aprobado") || p.Estado.Contains("Preparación")));
            return View();
        }
    }
}
