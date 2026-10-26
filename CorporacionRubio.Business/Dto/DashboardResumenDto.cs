using CorporacionRubio.Data.Entities;
using System.Collections.Generic;

namespace CorporacionRubio.Business.Dto
{
    public class DashboardResumenDto
    {
        public int TotalProductos { get; set; }
        public int TotalPedidos { get; set; }
        public int TotalAuditorias { get; set; }
        public int TotalCategorias { get; set; }
        public int TotalProveedores { get; set; }
        public int TotalMovimientos { get; set; }
        public int StockTotal { get; set; }
        public decimal ValorInventario { get; set; }
        public decimal TotalVentas { get; set; }
        public int ProductosStockCritico { get; set; }
        public List<Producto> ProductosCriticos { get; set; } = new();
        public List<Pedido> PedidosRecientes { get; set; } = new();
        public List<MovimientoInventario> MovimientosRecientes { get; set; } = new();
    }
}
