
using System.Collections.Generic;

namespace ABM.Models
{
    public class FirmaReporteViewModel
    {
        public Firma FirmaInfo { get; set; }
        public string FirmaUsuarioBase64 { get; set; } 
        public IEnumerable<DetalleFirma> DetallesFirma { get; set; }
    }
}