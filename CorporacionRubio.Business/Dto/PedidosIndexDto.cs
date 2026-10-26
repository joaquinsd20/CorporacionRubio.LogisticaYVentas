using CorporacionRubio.Data.Entities;
using System.Collections.Generic;

namespace CorporacionRubio.Business.Dto
{
    public class PedidosIndexDto
    {
        public List<Pedido> Pedidos { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();
    }
}
