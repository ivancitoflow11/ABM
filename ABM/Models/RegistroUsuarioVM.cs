using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class RegistroUsuarioVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        public string Apellidos { get; set; }
		[Required(ErrorMessage = "El RUT es obligatorio")]
		public string Rut { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Debe tener exactamente 8 dígitos")]
        public string Telefono { get; set; }


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
		[Required(ErrorMessage = "Los meses de expiracion son obligatorio")]
		[Range(1, 12, ErrorMessage = "Seleccione un valor válido para meses.")]
        public int MesesExpiracionClave { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int? RolId { get; set; }

        public IEnumerable<Rol> RolesDisponibles { get; set; }
    }
}
