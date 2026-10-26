using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CorporacionRubio.Data.Entities
{
    /// <summary>
    /// Usuario de seguridad ASP.NET Core Identity.
    /// Extiende IdentityUser con datos corporativos solicitados en el caso práctico.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? Nombres { get; set; }

        [MaxLength(100)]
        public string? Apellidos { get; set; }

        [MaxLength(120)]
        public string? NombreCompleto { get; set; }

        [MaxLength(80)]
        public string? Cargo { get; set; }

        [MaxLength(80)]
        public string? Area { get; set; }

        [MaxLength(120)]
        public string? Direccion { get; set; }

        [MaxLength(60)]
        public string? Ciudad { get; set; }

        [MaxLength(60)]
        public string? Pais { get; set; }

        [MaxLength(30)]
        public string Estado { get; set; } = "Activo";

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
