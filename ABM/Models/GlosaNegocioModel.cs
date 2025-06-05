using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class GlosaNegocioModel
    {
        [Required(ErrorMessage = "El campo Glosa es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo Glosa no puede exceder los 100 caracteres.")]
        public string GLOSA { get; set; }

        [StringLength(100, ErrorMessage = "El campo Negocio no puede exceder los 100 caracteres.")]
        public string NEGOCIO { get; set; }

        [StringLength(100, ErrorMessage = "El campo País no puede exceder los 100 caracteres.")]
        public string PAIS { get; set; }
    }
}