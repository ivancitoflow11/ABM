namespace ABM.Models
{
    // Representa un ticket/solicitud de soporte creado por un usuario dentro de ABM.
    public class Solicitud
    {
        public int idSolicitud { get; set; }
        public int idUsuarioCreador { get; set; }
        public string NombreCreador { get; set; }
        public string CorreoCreador { get; set; }

        public string asunto { get; set; }
        public string mensaje { get; set; }

        // "Pendiente" | "En curso" | "Completada"
        public string estado { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCambioEstado { get; set; }

        public int? idUsuarioActualizoEstado { get; set; }
        public string NombreActualizo { get; set; }
    }
}
