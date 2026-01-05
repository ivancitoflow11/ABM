using ABM.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; // IMPORTANTE: Necesario para 'Activity'
using ABM.Filters;
using Microsoft.AspNetCore.Authorization;
using ABM.Servicios;
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
        [Monitoreo("VisionCorporativa", "SELECT", "verVisionCorporativa")]
        public async Task<IActionResult> VisionCorporativa(int? idPais)
        {
            // 1) Leer idNegocio de sesión
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idNegocio = idNegocioSesion.Value;

            // 2) Traer países válidos
            var paises = (await repositorioVision.ObtenerListaPaisesPorNegocio(idNegocio))
                          ?.ToList()
                      ?? new List<PaisViewModel>();

            // ==============================================================================
            // CORRECCIÓN DEL ERROR AQUÍ
            // ==============================================================================
            if (!paises.Any())
            {
                // Pasamos el mensaje por ViewBag (la vista Error probablemente no espera 'message' en el modelo)
                ViewBag.ErrorMessage = "No hay países configurados para este negocio.";

                // Creamos el modelo REAL que espera la vista Error.cshtml
                var errorModel = new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                };

                return View("Error", errorModel);
            }
            // ==============================================================================

            // 3) Si llegó idPais válido, actúo en Post‐Redirect‐Get
            if (idPais.HasValue && paises.Any(p => p.IdPais == idPais.Value))
            {
                var nombrePais = paises.First(p => p.IdPais == idPais.Value).NombrePais;

                HttpContext.Session.SetInt32("IdPais", idPais.Value);
                HttpContext.Session.SetString("Pais", nombrePais);

                return RedirectToAction(nameof(VisionCorporativa));
            }

            // 4) Si no viene parámetro, leo el idPais ya en sesión (o el primero de la lista)
            int paisSeleccionado = HttpContext.Session.GetInt32("IdPais")
                                   ?? paises.First().IdPais;

            // Validación extra: si el país en sesión no pertenece a la lista actual (cambio de negocio), usar el primero
            if (!paises.Any(p => p.IdPais == paisSeleccionado))
            {
                paisSeleccionado = paises.First().IdPais;
            }

            // 5) Llamadas a repositorios
            var listaEvolucion = (await repositorioVision.ObtenerListaEvolucion(paisSeleccionado, idNegocio))?.ToList() ?? new List<EvolucionViewModel>();
            var listaRiesgoSistema = (await repositorioVision.ObtenerListaRiesgoSistema(paisSeleccionado, idNegocio))?.ToList() ?? new List<RiesgoSistemaViewModel>();
            var listaRiesgoPais = (await repositorioVision.ObtenerListaRiesgoPais(paisSeleccionado, idNegocio))?.ToList() ?? new List<RiesgoPaisViewModel>();
            var listaRiesgoGlobalParaElMapa = (await repositorioVision.ListaRiesgoPorNegocio(idNegocio))?.ToList() ?? new List<RiesgoPaisViewModel>();
            var listaSistema = (await repositorioVision.ListaDeSistemas(paisSeleccionado, idNegocio))?.ToList() ?? new List<Abm_Sistema>();
            var listaTendenciaDiaria = (await repositorioGestion.ObtenerListaTendenciaDiaria(paisSeleccionado, idNegocio))?.ToList() ?? new List<TendenciaDiariaViewModel>();
            var riesgoPerfil = (await repositorioVision.ObtenerRiesgoPerfil(paisSeleccionado, idNegocio))?.ToList() ?? new List<RiesgoSistemaViewModel>();

            // 6) Armar ViewModel
            var modelo = new VisionViewModel
            {
                ListaPaises = paises,
                SelectedPais = paisSeleccionado,
                ListaEvolucion = listaEvolucion,
                ListaRiesgoSistema = listaRiesgoSistema,
                ListaRiesgoPais = listaRiesgoPais,
                ListaRiesgoGlobalMapa = listaRiesgoGlobalParaElMapa,
                ListaSistema = listaSistema,
                ListaTendenciaDiaria = listaTendenciaDiaria,
                RiesgoPerfil = riesgoPerfil
            };

            return View(modelo);
        }

        [HttpGet]
        [Monitoreo("ObtenerGraficoDinamico", "SELECT", "obtenerGraficoDinamico")]
        public async Task<IActionResult> ObtenerGraficoDinamico(int sistema, int idPais)
        {
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idNegocioSesion.HasValue)
                return BadRequest("No hay negocio en sesión.");

            var datos = (await repositorioVision.ObtenerListaEvolucionParametro(
                         sistema, idPais, idNegocioSesion.Value))?.ToList()
                         ?? new List<TendenciaDiariaViewModel>();

            return Json(datos);
        }
    }
}