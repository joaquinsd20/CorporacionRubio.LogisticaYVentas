using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporacionRubio.Data.Entities
{
    [Table("Producto")]
    public class Producto
    {
        [Key]
        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(255)]
        public string? Descripcion { get; set; }

        [Column("precio_unitario")]
        public decimal? PrecioUnitario { get; set; }

        [Column("stock_actual")]
        public int? StockActual { get; set; }

        [Column("id_categoria")]
        public int? IdCategoria { get; set; }

        [Column("id_proveedor")]
        public int? IdProveedor { get; set; }

        public Categoria? Categoria { get; set; }
        public Proveedor? Proveedor { get; set; }
        public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }
}
