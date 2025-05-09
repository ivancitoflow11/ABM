namespace ABM.Models
{
   using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

    public class RolWizardViewModel
    {
        public int? IdRol { get; set; }
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [Display(Name = "Nombre del Rol")]
        public string NombreRol { get; set; }

        // Por ejemplo, una lista de privilegios (se pueden renderizar como checkboxes)
        [Display(Name = "Privilegios")]
        public List<string> Privilegios { get; set; } = new List<string>();


        [Display(Name = "Vista de Inicio")]
        public int? IdVistaInicio { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un País/Negocio/Sistema.")]
        [Display(Name = "Países, Negocios y Sistemas")]
        public List<int> ListaPNSSeleccionados { get; set; } = new List<int>();
        public List<int> ListaMenusSeleccionados { get; set; } = new List<int>();


        [Display(Name = "Vista")]
        public string VistaSeleccionada { get; set; }

        [Required(ErrorMessage = "Debes elegir un Menú de inicio")]
        [Display(Name = "Menú de inicio")]
        public int? MenuInicioSeleccionadoId { get; set; }


    }
}