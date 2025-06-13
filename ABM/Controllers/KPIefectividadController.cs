using ABM.Models;
using ABM.Filters;
using ABM.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using AutoMapper;
namespace ABM.Controllers
{
    [Authorize]
    public class KPIefectividadController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMapper mapper;
        private readonly IRepositorioKPI repositorioKPI;
        public KPIefectividadController(ILogger<HomeController> logger, IMapper Mapper, IRepositorioKPI RepositorioKPI)
        {
            _logger = logger;
            mapper = Mapper;
            repositorioKPI = RepositorioKPI;
        }
        [HttpGet]
        [Monitoreo("KPI_Efectividad_BU", "SELECT", "verKPIdiario")]
        public async Task<IActionResult> KPI_Efectividad_BU()
        {
            var resultados = await repositorioKPI.ObtenerKpiDiario();
            return View(resultados);
        }
    }
}