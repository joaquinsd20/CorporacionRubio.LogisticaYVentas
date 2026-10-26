using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CorporacionRubio.Data.Entities
{
    [Table("Pedido")]
    public class Pedido
    {
        [Key]
        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("fecha_pedido")]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("estado")]
        [StringLength(50)]
        public string? Estado { get; set; }

        public Usuario? Usuario { get; set; }
        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public ComprobantePagoPedido? PagoPedido { get; set; }

        [NotMapped]
        public decimal Total => DetallesPedido?.Sum(d => d.Subtotal ?? 0m) ?? 0m;
    }
}
