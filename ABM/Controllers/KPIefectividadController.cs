using ABM.Models;
using ABM.Filters;
using ABM.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System;
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
            var model = new KpiEfectividadViewModel();


            await PopulateMesesAniosDropdowns(model);


            model.Resultados = Enumerable.Empty<KpiResultado>();


            return View(model);
        }
        [HttpGet]
        [Monitoreo("KPI_Efectividad_BU", "SELECT", "seleccionarPeriodo")]
        public async Task<JsonResult> ObtenerMesesPorAnio(int anio)
        {
            var meses = await repositorioKPI.ObtenerMesesDisponiblesPorAnio(anio);
            var resultado = meses.Select(m => new SelectListItem
            {
                Value = m.mes.ToString(),
                Text = m.nombreMes
            });
            return Json(resultado);
        }
        [HttpPost]
        [Monitoreo("KPI_Efectividad_BU", "SELECT", "verKPIdiario")]
        public async Task<IActionResult> KPI_Efectividad_BU(KpiEfectividadViewModel model)
        {
            // Siempre poblar los dropdowns antes de devolver la vista
            await PopulateMesesAniosDropdowns(model);

            if (!ModelState.IsValid)
            {
                model.Resultados = Enumerable.Empty<KpiResultado>();
                return View(model);
            }

            // Obtener los resultados para el mes y año seleccionados por el usuario
            model.Resultados = await repositorioKPI.ObtenerKpiDiario(model.MesSeleccionado, model.AnioSeleccionado);
            return View(model);
        }

        // Método auxiliar para poblar los SelectLists
        private async Task PopulateMesesAniosDropdowns(KpiEfectividadViewModel model)
        {
            var mesesAniosDisponibles = (await repositorioKPI.ObtenerMesesAniosDisponibles()).ToList();

            // Populate Years
            model.Anios = mesesAniosDisponibles
                .Select(x => x.anio)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() });

            // Populate Months (considerando solo los meses para el año seleccionado si es necesario,
            // pero para un dropdown general, listar todos los meses numéricos)
            // Para mostrar nombres de meses, puedes usar un array o enum.
            var nombresMeses = new string[] { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                                               "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            // Filtramos los meses para que solo aparezcan los disponibles para el año seleccionado
            var mesesDelAnioSeleccionado = mesesAniosDisponibles
                .Where(x => x.anio == model.AnioSeleccionado)
                .Select(x => x.mes)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            model.Meses = mesesDelAnioSeleccionado
                .Select(m => new SelectListItem { Value = m.ToString(), Text = nombresMeses[m] });

            // Si el MesSeleccionado no está en los meses disponibles para el año seleccionado,
            // establece uno que sí lo esté (ej. el primero de la lista)
            if (!mesesDelAnioSeleccionado.Contains(model.MesSeleccionado) && mesesDelAnioSeleccionado.Any())
            {
                model.MesSeleccionado = mesesDelAnioSeleccionado.First();
            }
            else if (!mesesDelAnioSeleccionado.Any()) // Si no hay meses para el año, limpiar la selección
            {
                model.MesSeleccionado = 0; // O algún valor que indique "ninguno"
            }
        }
    }
}