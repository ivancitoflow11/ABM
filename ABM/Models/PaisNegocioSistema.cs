using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class PaisNegocioSistema
    {
        public int IdPaisNegocioSistema { get; set; } 
        public int IdSistema { get; set; }
        public int IdNegocio { get; set; }
        public int IdPais { get; set; }
        public string Estado { get; set; } // Se insertará "1" por defecto
    }
}
