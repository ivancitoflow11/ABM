// En Models/CambiarPasswordLogeadoViewModel.cs (o la ruta que uses para ViewModels)
using System.ComponentModel.DataAnnotations;

namespace ABM.Models // O ABM.ViewModels si esa es tu estructura
{
    public class CambiarPasswordLogeadoViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña Actual")]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos 14 caracteres.", MinimumLength = 14)]
        // Expresión regular actualizada para incluir 'ñ' y 'Ñ'
        [RegularExpression(@"^(?=.*[a-zñ])(?=.*[A-ZÑ])(?=.*\d)[A-Za-zñÑ\d\._\-!@#$%^&()+=]{14,}$",
            ErrorMessage = "La contraseña debe tener mínimo 14 caracteres, al menos una mayúscula, una minúscula  y un número.")]
        public string NuevaPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        [Compare("NuevaPassword", ErrorMessage = "La nueva contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmarNuevaPassword { get; set; }
    }
}