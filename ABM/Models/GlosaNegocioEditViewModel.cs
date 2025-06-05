using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class GlosaNegocioEditViewModel
    {
        [Required]
        public string GlosaOriginal { get; set; }

        [Required(ErrorMessage = "El campo Glosa es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo Glosa no puede exceder los 100 caracteres.")]
        [Display(Name = "Glosa")]
        public string Glosa { get; set; }

        [Display(Name = "País")]
        public string PaisIdSeleccionado { get; set; }

        [Display(Name = "Negocio")]
        public string NegocioNombreSeleccionado { get; set; }

        public List<SelectListItem> PaisesDisponibles { get; set; }
        public List<SelectListItem> NegociosDisponibles { get; set; }

        public GlosaNegocioEditViewModel()
        {
            PaisesDisponibles = new List<SelectListItem>();
            NegociosDisponibles = new List<SelectListItem> { new SelectListItem { Text = "Seleccione un país primero", Value = "" } };
        }
    }
}