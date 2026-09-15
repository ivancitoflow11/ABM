using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class SolicitudCrearViewModel
    {
        [Required(ErrorMessage = "El asunto es obligatorio.")]
        [StringLength(200, ErrorMessage = "El asunto no puede superar los 200 caracteres.")]
        [Display(Name = "Asunto")]
        public string Asunto { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [StringLength(4000, ErrorMessage = "El mensaje no puede superar los 4000 caracteres.")]
        [Display(Name = "Mensaje")]
        public string Mensaje { get; set; }
    }
}
