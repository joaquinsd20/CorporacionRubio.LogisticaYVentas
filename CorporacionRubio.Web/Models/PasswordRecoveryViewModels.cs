using System.ComponentModel.DataAnnotations;

namespace CorporacionRubio.Web.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese el correo registrado.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese el correo registrado.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El token de seguridad es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la nueva contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RestablecerAdminViewModel
    {
        [Required(ErrorMessage = "Ingrese el código de restablecimiento.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la nueva clave del administrador.")]
        [MinLength(6, ErrorMessage = "La clave debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string NuevaPassword { get; set; } = "123456";
    }
}
