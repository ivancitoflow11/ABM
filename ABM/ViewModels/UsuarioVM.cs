using ABM.Models;
using System.ComponentModel.DataAnnotations;

namespace ABM.ViewModels
{
    public class UsuarioVM
    {

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string correo { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string usuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string password_c { get; set; }
        public string password { get; set; }
        public string? estado { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("password_c", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarClave { get; set; }
        public int? idRol { get; set; }
        public int? ID_gerencia { get; set; }
        public int codVistaPrincipal { get; set; }
        public IEnumerable<Rol> Roles { get; set; }
        public string nuevoRol { get; set; }
        public IEnumerable<Menu> VistasMenu { get; set; }
        public IEnumerable<SubMenu> VistasSubMenu { get; set; }
        public List<int> PermisosMenuSeleccionados { get; set; } = new List<int>();
        public List<int> PermisosSubMenuSeleccionados { get; set; } = new List<int>();
        public IEnumerable<Abm_Sistema> Sistemas { get; set; }

        public IEnumerable<Gerencia> ListaGerencias { get; set; }
        public int IdUsuario { get; set; }

        public string firma { get; set; }

        public string firmaResponsable { get; set; }

        public IEnumerable<Subgerencia> ListaSubgerencias { get; set; }

        public int? ID_Subgerencia { get; set; }

        [Required(ErrorMessage = "Confirme si es o no responsable!")]
        public bool? ResponsableFirma { get; set; }
    }
}
