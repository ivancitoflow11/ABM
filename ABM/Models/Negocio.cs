using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class Negocio
    {
        public int IdNegocio { get; set; } 

        [Required(ErrorMessage = "El nombre del negocio es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [Display(Name = "Nombre del Negocio")]
        public string Nombre { get; set; }
    }
}