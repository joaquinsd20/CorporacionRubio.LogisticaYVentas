using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporacionRubio.Data.Entities
{
    [Table("Proveedor")]
    public class Proveedor
    {
        [Key]
        [Column("id_proveedor")]
        public int IdProveedor { get; set; }

        [Required]
        [Column("nombre_empresa")]
        [StringLength(100)]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Column("ruc")]
        [StringLength(15)]
        public string? Ruc { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
