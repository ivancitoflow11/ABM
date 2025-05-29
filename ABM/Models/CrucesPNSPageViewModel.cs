using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class CrucesPNSPageViewModel
    {
        public IEnumerable<CrucePNSViewModel> CrucesList { get; set; }
        public CrucePNSViewModel CruceParaCrear { get; set; } 
    }
}
