using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporacionRubio.Data.Entities
{
    [Table("Auditoria")]
    public class Auditoria
    {
        [Key]
        [Column("id_evento")]
        public int IdEvento { get; set; }

        [Column("tabla_afectada")]
        [StringLength(50)]
        public string? TablaAfectada { get; set; }

        [Column("accion")]
        [StringLength(20)]
        public string? Accion { get; set; }

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("valor_anterior")]
        [StringLength(200)]
        public string? ValorAnterior { get; set; }

        [Column("valor_nuevo")]
        [StringLength(200)]
        public string? ValorNuevo { get; set; }

        public Usuario? Usuario { get; set; }

        [NotMapped]
        public int IdAuditoria => IdEvento;
    }
}
