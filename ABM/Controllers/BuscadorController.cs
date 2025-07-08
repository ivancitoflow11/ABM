using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class BuscadorController : Controller
    {

        private readonly IRepositorioBuscador _repositorioBuscador;


        public BuscadorController(IRepositorioBuscador repositorioBuscador)
        {
            _repositorioBuscador = repositorioBuscador;
        }

        [HttpGet]
        public async Task<IActionResult> BuscadorAd(string employeeId, string displayName, string mail)
        {
            ViewBag.EmployeeId = employeeId;
            ViewBag.DisplayName = displayName;
            ViewBag.Mail = mail;

            List<AdUser> resultados;

            if (string.IsNullOrWhiteSpace(employeeId) &&
                string.IsNullOrWhiteSpace(displayName) &&
                string.IsNullOrWhiteSpace(mail))
            {
                resultados = new List<AdUser>();
            }
            else
            {
                var pEmployeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId;
                var pDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
                var pMail = string.IsNullOrWhiteSpace(mail) ? null : mail;


                resultados = (await _repositorioBuscador.BuscarUsuariosEnAd(pEmployeeId, pDisplayName, pMail)).ToList();
            }

            return View(resultados);
        }

        [HttpGet]
        public async Task<IActionResult> BuscadorFalanet(string rut, string apePaterno, string correo)
        {
            ViewBag.Rut = rut;
            ViewBag.ApePaterno = apePaterno;
            ViewBag.Correo = correo;

            List<ActivosFalanetUser> resultados;

            if (string.IsNullOrWhiteSpace(rut) &&
                string.IsNullOrWhiteSpace(apePaterno) &&
                string.IsNullOrWhiteSpace(correo))
            {
                resultados = new List<ActivosFalanetUser>();
            }
            else
            {
                resultados = (await _repositorioBuscador.BuscarEnActivosFalanet(rut, apePaterno, correo)).ToList();
            }

            return View(resultados);
        }
    }
}