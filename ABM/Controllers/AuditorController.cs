using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class AuditorController : Controller
    {
        private readonly IRepositorioAuditoriaFirmas repositorioAuditoriaFirmas;

        public AuditorController(IRepositorioAuditoriaFirmas repositorioAuditoriaFirmas)
        {
            this.repositorioAuditoriaFirmas = repositorioAuditoriaFirmas;
        }

        public async Task<IActionResult> AuditorFirmas(string fecha = "")
        {
            int mes, anio;

            if (string.IsNullOrEmpty(fecha))
            {
                mes = DateTime.Now.Month;
                anio = DateTime.Now.Year;
            }
            else
            {
                var partes = fecha.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[0], out mes) && int.TryParse(partes[1], out anio))
                {
                    // Se obtuvo el mes y el año correctamente
                }
                else
                {
                    mes = DateTime.Now.Month;
                    anio = DateTime.Now.Year;
                }
            }

            var auditoriaFirmas = await repositorioAuditoriaFirmas.ObtenerAuditoriaFirmas(mes, anio);
            ViewBag.MesSeleccionado = mes;
            ViewBag.AnioSeleccionado = anio;
            return View(auditoriaFirmas);
        }
    }
}
