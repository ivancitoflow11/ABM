using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class Rol
    {
        [Key]  // Esto define que idRol es la clave primaria
        public int idRol { get; set; }
        public string nombre { get; set; }
    }
}
