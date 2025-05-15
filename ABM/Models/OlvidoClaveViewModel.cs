using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class OlvidoClaveViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Correo { get; set; }
    }
}