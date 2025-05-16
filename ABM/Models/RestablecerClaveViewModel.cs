using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class RestablecerClaveViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos un minimo de 14 Caracteres!", MinimumLength = 14)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d\s:])(?!.*\s).{14,}$",
            ErrorMessage = "La contraseña debe tener al menos 14 caracteres, incluyendo al menos una mayúscula, una minúscula, un número y un símbolo especial (ej: !@#$%^&*). No debe contener espacios en blanco.")]
        [Display(Name = "Nueva Contraseña")]
        public string NuevaContrasena { get; set; }

        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "La nueva contraseña y la contraseña de confirmación no coinciden.")]
        [Display(Name = "Confirmar Nueva Contraseña")]
        public string ConfirmarContrasena { get; set; }
    }
}