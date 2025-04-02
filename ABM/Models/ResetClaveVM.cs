using System.ComponentModel.DataAnnotations;

namespace ABM.ViewModels
{
    public class ResetClaveVM
    {
        public string Token { get; set; }
        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{14,}$",
            ErrorMessage = "La contraseña debe tener al menos 14 caracteres, incluir una mayúscula, un número y puede contener caracteres especiales.")]
        public string NuevaClave { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("NuevaClave", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarClave { get; set; }
    }
}
