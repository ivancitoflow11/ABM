using Microsoft.AspNetCore.Mvc.Rendering;
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
        [Display(Name = "¿Es responsable de firmar? Marque la casilla si es SÍ")]
        public bool ResponsableFirma { get; set; }

        // Propiedad para recibir un archivo de firma NUEVO al editar.
        [Display(Name = "Cargar Nueva Firma (Opcional)")]
        public IFormFile NuevaFirma { get; set; }

        // Propiedad para guardar la RUTA de la firma que ya existe en la BD.
        // La usaremos para mostrar la imagen actual.
        public string FirmaActual { get; set; }
        public string Rut { get; set; }

        public int Telefono { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int? RolId { get; set; }

        public IEnumerable<Rol> RolesDisponibles { get; set; }

        [Display(Name = "Gerencia")]
        public int? IdGerencia { get; set; } 

        public IEnumerable<SelectListItem> GerenciasDisponibles { get; set; }
    }
}
