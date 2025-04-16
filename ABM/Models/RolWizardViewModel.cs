namespace ABM.Models
{
   using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

    public class RolWizardViewModel
    {
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [Display(Name = "Nombre del Rol")]
        public string NombreRol { get; set; }

        // Por ejemplo, una lista de privilegios (se pueden renderizar como checkboxes)
        [Display(Name = "Privilegios")]
        public List<string> Privilegios { get; set; } = new List<string>();


        [Display(Name = "Vista de Inicio")]
        public int? IdVistaInicio { get; set; }

        [Required(ErrorMessage = "Debe seleccionar país y negocio.")]
        [Display(Name = "País y Negocio")]
        public int? SelectedPaisNegocioSistemaId { get; set; }
        public List<int> ListaMenusSeleccionados { get; set; } = new List<int>();


        [Required(ErrorMessage = "Debe seleccionar la vista asociada.")]
        [Display(Name = "Vista")]
        public string VistaSeleccionada { get; set; }



    }
}