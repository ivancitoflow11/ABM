using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class GlosaNegociosViewModel
    {
        [Required(ErrorMessage = "El campo Glosa es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo Glosa no puede exceder los 100 caracteres.")]
        [Display(Name = "Nueva Glosa")]
        public string GlosaParaCrear { get; set; }

        [Display(Name = "País")]
        public string PaisIdSeleccionado { get; set; } // Almacenará el ID del país

        [Display(Name = "Negocio")]
        public string NegocioNombreSeleccionado { get; set; } // Almacenará el NOMBRE del negocio

        public List<SelectListItem> PaisesDisponibles { get; set; }
        public List<SelectListItem> NegociosDisponibles { get; set; }

        public IEnumerable<GlosaNegocioModel> ListadoGlosas { get; set; }

        public GlosaNegociosViewModel()
        {
            PaisesDisponibles = new List<SelectListItem>();
            NegociosDisponibles = new List<SelectListItem> { new SelectListItem { Text = "Seleccione un país primero", Value = "" } };
            ListadoGlosas = new List<GlosaNegocioModel>();
        }
    }
}