using ABM.Servicios; 
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    public class ResumenController : Controller
    {
        private readonly IRepositorioResumen _repositorio;

        public ResumenController(IRepositorioResumen repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<IActionResult> Index(DateTime? fechaBusqueda)
        {
            // 1. Obtener los datos filtrados (tu lógica actual)
            var datos = await _repositorio.ObtenerResumenSistemas(fechaBusqueda);

            // 2. Obtener TODAS las fechas disponibles para pintar el calendario
            var fechasDb = await _repositorio.ObtenerFechasDisponibles();

            // 3. Convertirlas a una lista de strings "yyyy-MM-dd" para JavaScript
            // Esto se lo pasaremos al calendario para que sepa qué días habilitar
            ViewBag.ListaFechasDisponibles = fechasDb.Select(f => f.ToString("yyyy-MM-dd")).ToList();

            // 4. Manejo de la fecha seleccionada actual (igual que antes)
            if (fechaBusqueda.HasValue)
            {
                ViewBag.FechaSeleccionada = fechaBusqueda.Value.ToString("yyyy-MM-dd");
            }
            else if (datos.Any())
            {
                ViewBag.FechaSeleccionada = datos.First().Fecha_Reporte.ToString("yyyy-MM-dd");
            }

            return View(datos);
        }
    }
}