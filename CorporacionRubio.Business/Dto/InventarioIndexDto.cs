using CorporacionRubio.Data.Entities;
using System.Collections.Generic;

namespace CorporacionRubio.Business.Dto
{
    public class InventarioIndexDto
    {
        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public List<Proveedor> Proveedores { get; set; } = new();
        public List<MovimientoInventario> MovimientosRecientes { get; set; } = new();
    }
}
