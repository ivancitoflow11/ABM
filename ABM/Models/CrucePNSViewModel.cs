using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class CrucePNSViewModel
    {
        // Para ftc_pais_negocio_sistema
        public int IdPaisNegocioSistema { get; set; }

        [Required(ErrorMessage = "El campo País es obligatorio.")]
        [Display(Name = "País")]
        public int IdPais { get; set; }

        [Required(ErrorMessage = "El campo Negocio es obligatorio.")]
        [Display(Name = "Negocio")]
        public int IdNegocio { get; set; }

        [Required(ErrorMessage = "El campo Sistema es obligatorio.")]
        [Display(Name = "Sistema")]
        public int IdSistema { get; set; }

        public string? Estado { get; set; } // Se cargará en la edición

        // Para ftc_pnsjt
        public int Id_pns { get; set; } // PK de ftc_pnsjt, útil para la edición

        [Required(ErrorMessage = "El campo Nombre tabla es obligatorio.")]
        [StringLength(20, ErrorMessage = "El Nombre tabla no puede exceder los 20 caracteres.")]
        [Display(Name = "Nombre tabla")]
        public string Tabla { get; set; }

        [Required(ErrorMessage = "El campo Ruta (Trans) es obligatorio.")]
        [StringLength(100, ErrorMessage = "La Ruta (Trans) no puede exceder los 100 caracteres.")]
        [Display(Name = "Ruta (Trans)")] 
        public string Trans { get; set; }

        [Required(ErrorMessage = "El campo IP Máquina es obligatorio.")]
        [StringLength(15, ErrorMessage = "La IP Máquina no puede exceder los 15 caracteres.")]
        [Display(Name = "IP Máquina")]
        public string Ip { get; set; }

        [Required(ErrorMessage = "El campo Responsable es obligatorio.")]
        [StringLength(30, ErrorMessage = "El Responsable no puede exceder los 30 caracteres.")]
        public string Responsable { get; set; }

        // Para Dropdowns en las vistas
        public IEnumerable<SelectListItem>? Paises { get; set; }
        public IEnumerable<SelectListItem>? Negocios { get; set; }
        public IEnumerable<SelectListItem>? Sistemas { get; set; }

        // para mostrar nombres en la vista de listado
        public string? NombrePais { get; set; }
        public string? NombreNegocio { get; set; }
        public string? NombreSistema { get; set; }
    }
}
