using System.Threading.Tasks;
using ABM.Filters;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class HistorialController : Controller
    {
        private readonly IRepositorioMatriz _repositorioMatriz;

        public HistorialController(IRepositorioMatriz repositorioMatriz)
        {
            _repositorioMatriz = repositorioMatriz;
        }

        [HttpGet]
        [Monitoreo("Historial", "SELECT", "verHistorialFirmas")]
        public async Task<IActionResult> Index()
        {
            var todasLasFirmas = await _repositorioMatriz.ObtenerTodasLasFirmas();
            return View(todasLasFirmas);
        }
    }
}