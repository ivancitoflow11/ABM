
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class CargarArchivosViewModel
    {

        [Required(ErrorMessage = "Debe seleccionar un País.")]
        public string PaisSeleccionado { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Negocio.")]
        public string NegocioSeleccionado { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una fecha.")]
        [DataType(DataType.Date)]
        public DateTime FechaCarga { get; set; } = DateTime.Today;

        public SelectList Paises { get; set; }
        public SelectList Negocios { get; set; }


        public IEnumerable<HistoricoCargaKPI> HistoricoCargas { get; set; }

        public CargarArchivosViewModel()
        {
            HistoricoCargas = new List<HistoricoCargaKPI>();
            Paises = new SelectList(new List<SelectListItem>());
            Negocios = new SelectList(new List<SelectListItem>());
        }
    }
}