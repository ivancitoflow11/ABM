using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; 

namespace ABM.Models
{
    public class KpiEfectividadViewModel
    {
        [Display(Name = "Mes")]
        [Required(ErrorMessage = "El mes es obligatorio.")]
        public int MesSeleccionado { get; set; }

        [Display(Name = "Año")]
        [Required(ErrorMessage = "El año es obligatorio.")]
        public int AnioSeleccionado { get; set; }

        public IEnumerable<SelectListItem> Meses { get; set; }
        public IEnumerable<SelectListItem> Anios { get; set; }

        public IEnumerable<KpiResultado> Resultados { get; set; }
    }
}