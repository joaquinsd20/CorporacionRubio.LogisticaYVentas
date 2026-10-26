using CorporacionRubio.Business.Dto;
using CorporacionRubio.Data.Entities;

namespace CorporacionRubio.Web.Models
{
    public class PanelLogisticoViewModel
    {
        public DashboardResumenDto Resumen { get; set; } = new();
        public InventarioIndexDto Inventario { get; set; } = new();
        public PedidosIndexDto Pedidos { get; set; } = new();
    }

    public class PanelVentasViewModel
    {
        public DashboardResumenDto Resumen { get; set; } = new();
        public PedidosIndexDto Pedidos { get; set; } = new();
    }

    public class PortalClienteViewModel
    {
        public string NombreCliente { get; set; } = "Cliente";
        public List<Producto> Productos { get; set; } = new();
        public List<Pedido> MisPedidos { get; set; } = new();
    }

    public class LandingCatalogViewModel
    {
        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public int StockTotal { get; set; }
        public decimal ValorCatalogo { get; set; }
    }
}
