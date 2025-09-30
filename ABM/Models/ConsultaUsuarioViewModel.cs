using System.Collections.Generic;

namespace ABM.Models
{
    public class ConsultaUsuarioViewModel
    {
        public string Input { get; set; }
        public DatosBasicosUsuario? DatosBasicos { get; set; }
        public List<EstadoUsuario> Estados { get; set; } = new();
        public string Mensaje { get; set; } = "";
        public bool TieneProblemas { get; set; } = false;
    }

    public class DatosBasicosUsuario
    {
        public string Nombre { get; set; }
        public string Correo { get; set; }
        // Si quieres mostrar RUT u Origen después, agrégalos aquí.
    }

    public class EstadoUsuario
    {
        public string NombreEstado { get; set; }  // SPR | AD | FINIQUITADO
        public string Valor { get; set; }         // ACTIVO | SI | NO | etc.
    }
}
