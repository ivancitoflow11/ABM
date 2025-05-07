using ABM.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ABM.Servicios;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ABM.ViewModels;

namespace ABM.Controllers
{
	[Authorize]
	public class VisionController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IRepositorioUsuarios repositorioUsuarios;
		private readonly IMapper mapper;
		private readonly IRepositorioRoles repositorioRoles;
		private readonly IRepositorioGestion repositorioGestion;
		private readonly IRepositorioVision repositorioVision;

		public VisionController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles, IRepositorioGestion RepositorioGestion, IRepositorioVision RepositorioVision)
		{
			_logger = logger;
			repositorioUsuarios = RepositorioUsuarios;
			mapper = Mapper;
			repositorioRoles = RepositorioRoles;
			repositorioGestion = RepositorioGestion;
			repositorioVision = RepositorioVision;
		}


        [HttpGet]
        public async Task<IActionResult> VisionCorporativa(int? idPais)
        {
            // 1) idNegocio en sesión
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");
            int idNegocio = idNegocioSesion.Value;

            // 2) Listar países
            var paises = (await repositorioVision.ObtenerListaPaisesPorNegocio(idNegocio)).ToList();
            if (!paises.Any())
                return View("Error", new { message = "No hay países para este negocio." });

            // 3) Si llegó idPais válido, guardar y redirigir (Post‐Redirect‐Get)
            if (idPais.HasValue && paises.Any(p => p.IdPais == idPais.Value))
            {
                var nombrePais = paises.First(p => p.IdPais == idPais.Value).NombrePais;

                HttpContext.Session.SetInt32("IdPais", idPais.Value);
                HttpContext.Session.SetString("Pais", nombrePais);
                // HttpContext.Session.SetString("Negocio", nombreNegocioSesion);

                return RedirectToAction(nameof(VisionCorporativa));
            }

            // 4) Si no llegó idPais o es inválido, leo el que ya esté en sesión
            int paisSeleccionado = HttpContext.Session.GetInt32("IdPais")
                                   ?? paises.First().IdPais;

            // 5) Ahora construyo el ViewModel con ese paisSeleccionado
            var modelo = new VisionViewModel
            {
                ListaPaises = paises,
                SelectedPais = paisSeleccionado,
                ListaEvolucion = await repositorioVision.ObtenerListaEvolucion(paisSeleccionado, idNegocio),
                ListaRiesgoSistema = await repositorioVision.ObtenerListaRiesgoSistema(paisSeleccionado, idNegocio),
                ListaRiesgoPais = await repositorioVision.ObtenerListaRiesgoPais(paisSeleccionado, idNegocio),
                ListaSistema = await repositorioVision.ListaDeSistemas(paisSeleccionado, idNegocio),
                ListaTendenciaDiaria = await repositorioGestion.ObtenerListaTendenciaDiaria(paisSeleccionado, idNegocio),
                RiesgoPerfil = await repositorioVision.ObtenerRiesgoPerfil(paisSeleccionado, idNegocio)
            };

            return View(modelo);
        }


        [HttpGet]
		public async Task<IActionResult> ObtenerGraficoDinamico(int sistema, int idPais)
		{
			var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
			if (!idNegocioSesion.HasValue)
				return BadRequest("No hay negocio en sesión.");

			var datos = await repositorioVision.ObtenerListaEvolucionParametro(
				sistema, idPais, idNegocioSesion.Value);

			return Json(datos);
		}

	}
}
