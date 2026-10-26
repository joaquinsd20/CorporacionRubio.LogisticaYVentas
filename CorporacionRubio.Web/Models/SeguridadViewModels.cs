using System.ComponentModel.DataAnnotations;

namespace CorporacionRubio.Web.Models
{
    public class SeguridadIndexViewModel
    {
        public List<UsuarioSeguridadItemViewModel> Usuarios { get; set; } = new();
        public List<string> Roles { get; set; } = new();

        [Display(Name = "Nuevo rol")]
        public string? NuevoRol { get; set; }
    }

    public class UsuarioSeguridadItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string RolPrincipal { get; set; } = string.Empty;
        public string RolesTexto { get; set; } = string.Empty;
        public bool Bloqueado { get; set; }
        public bool EmailConfirmado { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
