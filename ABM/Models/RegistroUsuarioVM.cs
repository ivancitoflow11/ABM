using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class RegistroUsuarioVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        public string Apellidos { get; set; }

        public string Rut { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Range(1, 9999999999, ErrorMessage = "Solamente se permiten números (máximo 10 dígitos)")]
        public int Telefono { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[A-Z])(?=.*\d).{8,50}$",
            ErrorMessage = "La contraseña debe contener al menos una letra, una mayúscula, un número y puede incluir caracteres especiales.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe repetir la contraseña.")]
        public string Repeat_Password { get; set; }

        [Range(1, 12, ErrorMessage = "Seleccione un valor válido para meses.")]
        public int MesesExpiracionClave { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int? RolId { get; set; }

        public IEnumerable<Rol> RolesDisponibles { get; set; }
    }
}
