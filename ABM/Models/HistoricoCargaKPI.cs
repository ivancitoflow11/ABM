
using System;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class HistoricoCargaKPI
    {
        public int IdCarga { get; set; } 

        [Required]
        public int IdCrucesKPI { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un archivo como minimo.")]
        public string Archivo { get; set; } 

        [Required(ErrorMessage = "Debe seleccionar una fecha de carga.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

  
        public string NombrePais { get; set; }
        public string NombreNegocio { get; set; }
    }
}