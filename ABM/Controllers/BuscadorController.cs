using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABM.Filters;
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
        [Monitoreo("Buscador", "SELECT", "verPaginaBuscador")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Monitoreo("BuscadorPasoSap", "SELECT", "buscarUsuariosEnPasoSap")]
        public async Task<IActionResult> BuscadorPasoSap(string rutODni, string nombreCompleto, string correoUsuario)
        {
            ViewBag.RutODni = rutODni;
            ViewBag.NombreCompleto = nombreCompleto;
            ViewBag.CorreoUsuario = correoUsuario;

            List<SapUser> resultados;

            if (string.IsNullOrWhiteSpace(rutODni) &&
                string.IsNullOrWhiteSpace(nombreCompleto) &&
                string.IsNullOrWhiteSpace(correoUsuario))
            {
                resultados = new List<SapUser>();
            }
            else
            {
                resultados = (await _repositorioBuscador.BuscarEnPasoSap(rutODni, nombreCompleto, correoUsuario)).ToList();
            }

            return PartialView("_BuscadorPasoSap", resultados);
        }
        [HttpGet]
        [Monitoreo("BuscadorFiniquitados", "SELECT", "buscarUsuariosFiniquitados")]
        public async Task<IActionResult> BuscadorFiniquitados(string rutDni, string nombreUsuario, string mailUsuario)
        {
            // Obtenemos los datos de la sesión, como en tus otros controladores
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            // Validamos que la sesión exista
            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
            {
                // Puedes manejar este error como prefieras, aquí solo devuelvo un mensaje.
                return PartialView("_ErrorSesion");
            }

            ViewBag.RutDni = rutDni;
            ViewBag.NombreUsuario = nombreUsuario;
            ViewBag.MailUsuario = mailUsuario;

            List<FiniquitadoUser> resultados;

            if (string.IsNullOrWhiteSpace(rutDni) &&
                string.IsNullOrWhiteSpace(nombreUsuario) &&
                string.IsNullOrWhiteSpace(mailUsuario))
            {
                resultados = new List<FiniquitadoUser>();
            }
            else
            {
                resultados = (await _repositorioBuscador.BuscarEnFiniquitados(
                    rutDni,
                    nombreUsuario,
                    mailUsuario,
                    idPaisSesion.Value,
                    idNegocioSesion.Value
                )).ToList();
            }

            return PartialView("_BuscadorFiniquitados", resultados);
        }
        [HttpGet]
        [Monitoreo("BuscadorAd", "SELECT", "buscarUsuariosEnAd")]
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
                // La lógica de la consulta no cambia.
                resultados = (await _repositorioBuscador.BuscarUsuariosEnAd(employeeId, displayName, mail)).ToList();
            }

            // Devuelve la vista parcial con el modelo. El nombre debe coincidir con el archivo .cshtml parcial.
            return PartialView("_BuscadorAd", resultados);
        }


        // Reemplaza este método en tu BuscadorController.cs
        [HttpGet]
        [Monitoreo("BuscadorFalanet", "SELECT", "buscarUsuariosEnFalanet")]
        public async Task<IActionResult> BuscadorFalanet(string rut, string nombreCompleto, string correo)
        {
            ViewBag.Rut = rut;
            ViewBag.NombreCompleto = nombreCompleto; // Cambio aquí
            ViewBag.Correo = correo;

            List<ActivosFalanetUser> resultados;

            if (string.IsNullOrWhiteSpace(rut) &&
                string.IsNullOrWhiteSpace(nombreCompleto) && // Cambio aquí
                string.IsNullOrWhiteSpace(correo))
            {
                resultados = new List<ActivosFalanetUser>();
            }
            else
            {
                resultados = (await _repositorioBuscador.BuscarEnActivosFalanet(rut, nombreCompleto, correo)).ToList(); // Cambio aquí
            }

            return PartialView("_BuscadorFalanet", resultados);
        }
        // Añade este método a tu BuscadorController.cs
        [HttpGet]
        [Monitoreo("BuscadorAgrupaActivos", "SELECT", "buscarUsuariosEnAgrupaActivos")]
        public async Task<IActionResult> BuscadorAgrupaActivos(string rutDni, string nombreUsuario, string mailUsuario)
        {
            ViewBag.RutDni = rutDni;
            ViewBag.NombreUsuario = nombreUsuario;
            ViewBag.MailUsuario = mailUsuario;

            List<AgrupaActivosUser> resultados;

            if (string.IsNullOrWhiteSpace(rutDni) &&
                string.IsNullOrWhiteSpace(nombreUsuario) &&
                string.IsNullOrWhiteSpace(mailUsuario))
            {
                resultados = new List<AgrupaActivosUser>();
            }
            else
            {
                resultados = (await _repositorioBuscador.BuscarEnAgrupaActivos(rutDni, nombreUsuario, mailUsuario)).ToList();
            }

            return PartialView("_BuscadorAgrupaActivos", resultados);
        }
    }
}