using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class ListaBlanca
    {
        [Display(Name = "RUT/Núm. Documento")]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(100)]
        public string RutNumDocumento { get; set; } // PK

        [StringLength(100)]
        public string RUT { get; set; }

        [StringLength(2)]
        public string DV { get; set; }

        [Display(Name = "Número Empleado")]
        [StringLength(100)]
        public string NumeroEmpleado { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        public string Nombres { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        public string Apellidos { get; set; }

        [Display(Name = "Apellido Nombre")]
        [StringLength(200)]
        public string ApellidoNombre { get; set; } 

        [StringLength(100)]
        public string Negocio { get; set; }

        [StringLength(100)]
        public string Pais { get; set; }

        [StringLength(100)]
        public string Cargo { get; set; }

        [StringLength(100)]
        public string Departamento { get; set; }

        [Display(Name = "Tipo Empleado (Interno/Externo)")]
        [StringLength(100)]
        public string TipoEmpleado { get; set; }

        [Display(Name = "Activo Falanet")]
        public bool ActivoFalanet { get; set; }

        [Display(Name = "Finiquitados Falanet")]
        public bool FiniquitadosFalanet { get; set; }

        [Display(Name = "Activo AD")]
        public bool ActivoAd { get; set; }
    }

    public class ListaBlancaViewModel : ListaBlanca
    {

    }
}