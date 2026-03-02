using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.API.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder los 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Admin|Cliente)$", ErrorMessage = "El rol debe ser 'Admin' o 'Cliente'.")]
        public string Rol { get; set; } = "Cliente";
    }
}