
using System;

namespace ABM.Models
{
    public class EnvioCorreoLista
    {
        public int IdCorreos { get; set; } 

        public string? NombreLista { get; set; } 

        public string? Envio_Diario { get; set; } 
        public string? Envio_Semanal { get; set; }
        public string? Envio_Gerente { get; set; }
        public string? Envio_Mensual { get; set; }
        public int IdDetalleCorreo { get; set; }
        public string? Envio_Quincenal { get; set; }
        public string? Envio_Jefe { get; set; }

        public string? Tipo_Carga { get; set; }
        public DateTime? Fecha_Ultima_Carga { get; set; } 
    }
}