using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class Pnsjt
    {
        public int Id_pns { get; set; }
        public string Tabla { get; set; }
        public string? Trans { get; set; } // Corresponde a "Ruta" en el formulario
        public int IdPaisNegocioSistema { get; set; }
        public string Ip { get; set; }
        public string Responsable { get; set; }
    }
}
