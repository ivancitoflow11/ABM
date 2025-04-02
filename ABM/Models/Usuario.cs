using Microsoft.AspNetCore.Identity;

namespace ABM.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string nombre { get; set; }
        public string correo { get; set; }
        public string password_c { get; set; }
        public string password { get; set; }
        public string TokenRecuperacion { get; set; } // Campo para token
        public DateTime? ExpiracionToken { get; set; }
        // Propiedad de foreign key
        public int? idRol { get; set; }
        public int? ID_gerencia { get; set; }
        public string? estado { get; set; }

        // Relación con la tabla Rol
        public Rol Rol { get; set; }
        public virtual Gerencia Gerencia { get; set; }

        public string firma { get; set; }
        public int? ID_Subgerencia { get; set; }
        public Subgerencia Subgerencia { get; set; }
        public bool? ResponsableFirma { get; set; }

    }
}