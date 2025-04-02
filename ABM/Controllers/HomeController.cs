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
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IRepositorioAlertaSistema repositorioAlertaSistema;
        private readonly IRepositorioGestion repositorioGestion;
        private readonly IMapper mapper;
        private readonly IRepositorioVision repositorioVision;
        private readonly IRepositorioCargas repositorioCargas;
        private readonly IRepositorioReportes repositorioReportes;
        public HomeController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IRepositorioAlertaSistema RepositorioAlertaSistema, IRepositorioGestion RepositorioGestion, IMapper Mapper, IRepositorioVision RepositorioVision, IRepositorioCargas RepositorioCargas, IRepositorioReportes repositorioReportes)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            repositorioAlertaSistema = RepositorioAlertaSistema;
            repositorioGestion = RepositorioGestion;
            mapper = Mapper;
            repositorioVision = RepositorioVision;
            repositorioCargas = RepositorioCargas;
            this.repositorioReportes = repositorioReportes;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }

        public async Task<IActionResult> ListaUsuarios()
        {
            var usuarios = await repositorioUsuarios.ObtenerTodosLosUsuarios();

            return View(usuarios);
        }

        public async Task<IActionResult> ListaRoles()
        {
            var modelo = await repositorioUsuarios.ObtenerRoles();

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> ListaRoles(int idRol)
        {
            var modelo = await repositorioUsuarios.ObtenerRoles();
            // Establecer ViewBag.RolSeleccionado con el nombre del rol seleccionado
            ViewBag.RolSeleccionado = modelo.FirstOrDefault(r => r.idRol == idRol)?.nombre;

            // Redirigir a la vista EditarPermisos
            return RedirectToAction("EditarPermisos", new { idRol });
        }

        public async Task<IActionResult> EditarPermisos(int idRol)
        {
            PermisosViewModel modelo = new PermisosViewModel();
            modelo.PermisosMenu = await repositorioUsuarios.ObtenerPermisosMenuPorRol(idRol);
            modelo.PermisosSubMenu = await repositorioUsuarios.ObtenerPermisosSubMenuPorRol(idRol);
            modelo.Menu = await repositorioUsuarios.ObtenerMenu();
            modelo.Submenu = await repositorioUsuarios.ObtenerSubMenu();
            return View(modelo);
        }

        public async Task<IActionResult> AlertaSistema()
        {
            AlertaSistemaViewModel modelo = new AlertaSistemaViewModel();

            modelo.estadisticas = await repositorioAlertaSistema.ObtenerEstadisticasUsuarios();
            modelo.ListaFiltroFiniquitados = await repositorioAlertaSistema.ObtenerDetalleFiniquitados();
            modelo.ListaUsuariosNoEncontrados = await repositorioAlertaSistema.ObtenerDetalleNoEncontrados();
            modelo.ListaUsuariosDuplicados = await repositorioAlertaSistema.ObtenerDetalleDuplicados();

            return View(modelo);
        }



        public async Task<IActionResult> VisionCorporativa()
        {
            VisionViewModel modelo = new VisionViewModel();
            modelo.ListaEvolucion = await repositorioVision.ObtenerListaEvolucion();
            modelo.ListaRiesgoSistema = await repositorioVision.ObtenerListaRiesgoSistema();
            modelo.ListaRiesgoPais = await repositorioVision.ObtenerListaRiesgoPais();
            modelo.ListaSistema = await repositorioCargas.ListaDeSistemas();
            modelo.ListaTendenciaDiaria = await repositorioGestion.ObtenerListaTendenciaDiaria();
            modelo.RiesgoPerfil = await repositorioVision.ObtenerRiesgoPerfil();
            return View(modelo);
        }

        public async Task<IActionResult> Gestion()
        {
            ResumenGestionViewModel modelo2 = await repositorioGestion.ObtenerDatosGestion();
            var modelo = mapper.Map<GestionViewModel>(modelo2);
            modelo.ListaCasosCargo = await repositorioGestion.ObtenerListaCasosCargo();
            modelo.ListaEvidenciasFiniquitado = await repositorioGestion.ObtenerListaEvidenciasFiniquitado();
            modelo.ListaResumenPais = await repositorioGestion.ObtenerListaResumenPais();
            modelo.ListaTendenciaDiaria = await repositorioGestion.ObtenerListaTendenciaDiaria();
            return View(modelo);
        }
        public async Task<IActionResult> ObtenerGraficoDinamico(int sistema)
        {
            var datosGrafico = await repositorioVision.ObtenerListaEvolucionParametro(sistema);

            // Devolver los datos como JSON
            return Json(datosGrafico);
        }

        public async Task<IActionResult> ObtenerUltimaFecha()
        {
            // Obtener la última fecha de carga desde el repositorio
            DateTime? ultimaFechaDeCarga = await repositorioCargas.ObtenerUltimaFechaDeCarga();

            // Pasar la fecha de carga a la vista usando ViewBag o ViewData
            ViewBag.UltimaFechaDeCarga = ultimaFechaDeCarga;

            // Devolver la vista correspondiente
            return View();
        }

    }
}
