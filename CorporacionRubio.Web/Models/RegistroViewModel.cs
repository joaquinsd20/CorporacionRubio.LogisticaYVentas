using System.ComponentModel.DataAnnotations;

namespace CorporacionRubio.Web.Models
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "Ingrese los nombres.")]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [MaxLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese el correo.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione un perfil.")]
        public string Rol { get; set; } = "Cliente";

        [MaxLength(80)]
        public string? Cargo { get; set; }

        [MaxLength(80)]
        public string? Area { get; set; }

        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
