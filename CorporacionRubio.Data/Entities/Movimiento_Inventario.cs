using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporacionRubio.Data.Entities
{
    [Table("Movimiento_Inventario")]
    public class MovimientoInventario
    {
        [Key]
        [Column("id_movimiento")]
        public int IdMovimiento { get; set; }

        [Required]
        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Required]
        [Column("tipo_movimiento")]
        [StringLength(20)]
        public string TipoMovimiento { get; set; } = string.Empty;

        [Required]
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("fecha_movimiento")]
        public DateTime FechaMovimiento { get; set; } = DateTime.Now;

        [Required]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        public Producto? Producto { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
