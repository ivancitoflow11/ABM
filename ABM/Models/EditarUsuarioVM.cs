using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class EditarUsuarioVM
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        public string Apellidos { get; set; }

        public string Rut { get; set; }

        public string Telefono { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int? RolId { get; set; }

        public IEnumerable<Rol> RolesDisponibles { get; set; }
    }
}
