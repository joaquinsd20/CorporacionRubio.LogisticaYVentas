using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporacionRubio.Data.Entities
{
    [Table("Comprobante_Pago_Pedido")]
    public class ComprobantePagoPedido
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        [Required]
        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Required]
        [StringLength(40)]
        [Column("metodo_pago")]
        public string MetodoPago { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Column("referencia_operacion")]
        public string ReferenciaOperacion { get; set; } = string.Empty;

        [StringLength(40)]
        [Column("tipo_comprobante")]
        public string? TipoComprobante { get; set; }

        [StringLength(40)]
        [Column("estado_pago")]
        public string EstadoPago { get; set; } = "Pendiente de validación";

        [StringLength(200)]
        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("fecha_validacion")]
        public DateTime? FechaValidacion { get; set; }

        [Column("id_usuario_validacion")]
        public int? IdUsuarioValidacion { get; set; }

        public Pedido? Pedido { get; set; }
    }
}
