using System.ComponentModel.DataAnnotations;

namespace CorporacionRubio.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo corporativo.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool Recordarme { get; set; }
    }
}
