using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class CambioPasswordVM
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos 14 caracteres.", MinimumLength = 14)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d\._\-!@#$%^&()+=]{14,}$",
    ErrorMessage = "La contraseña debe contener mínimo 14 caracteres, al menos una letra mayúscula y un número.")]
        public string NuevaContrasena { get; set; }


        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }

    }
}