
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class Gerencia
    {
        public int IdGerencia { get; set; } 

        [Required(ErrorMessage = "El nombre de la gerencia es obligatorio.")]
        [StringLength(255, ErrorMessage = "El nombre no puede exceder los 255 caracteres.")]
        [Display(Name = "Nombre Gerencia")]
        public string Nom_Gerencia { get; set; }

        [StringLength(255, ErrorMessage = "El sistema no puede exceder los 255 caracteres.")]
        public string sistema { get; set; } 
    }
}