using ABM.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ABM.Filters;
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
    public class AlertasController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;
        private readonly IRepositorioRoles repositorioRoles;
        private readonly IRepositorioAlertas repositorioAlertas;

        public AlertasController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles, IRepositorioAlertas RepositorioAlertas)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
            repositorioRoles = RepositorioRoles;
            repositorioAlertas = RepositorioAlertas;
        }

        [HttpGet]
        [Monitoreo("AlertaSistema", "SELECT", "verAlertaSistema")]
        public async Task<IActionResult> AlertaSistema(int? idNegocio, int? idSistema, bool buscar = false)
        {
            var modelo = new AlertaSistemaViewModel();

            var usuario = await repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            var rolConPNS = (await repositorioRoles.ObtenerRolesConPNS())
                                     .FirstOrDefault(r => r.idRol == usuario.idRol);

            List<PaisNegocioViewModel> listaNegocios = new();
            if (rolConPNS?.PaisesNegocios != null)
            {
                listaNegocios = rolConPNS.PaisesNegocios
                    .GroupBy(x => x.idNegocio)
                    .Select(g => g.First())
                    .ToList();
            }
            ViewBag.Negocios = listaNegocios;


            ViewBag.Sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);

            ViewBag.IdNegocioSeleccionado = idNegocio;
            ViewBag.IdSistemaSeleccionado = idSistema;


            if (buscar)
            {

                var estadisticasCompletas = await repositorioAlertas.ObtenerEstadisticasUsuarios(idNegocio, idSistema);

                modelo.estadisticas = estadisticasCompletas;
                modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados(idNegocio, idSistema);
                modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados(idNegocio, idSistema);
                modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados(idNegocio, idSistema);


                modelo.PaisesEnCabecera = estadisticasCompletas
                    .Select(e => new PaisViewModel { pais = e.Pais, Bandera = e.Bandera })
                    .GroupBy(p => p.pais)
                    .Select(g => g.First())
                    .OrderBy(p => p.pais)
                    .ToList();

                modelo.EstadisticasAgrupadas = estadisticasCompletas
                    .GroupBy(e => new { e.Vertical, e.Negocio, e.Sistema })
                    .ToList();
            }
            else
            {
          
                modelo.estadisticas = new List<EstadisticasUsuarios>();
                modelo.ListaFiltroFiniquitados = new List<Finiquitados>();
                modelo.ListaUsuariosNoEncontrados = new List<UsuariosNoEncontrados>();
                modelo.ListaUsuariosDuplicados = new List<UsuariosDuplicados>();
                modelo.PaisesEnCabecera = new List<PaisViewModel>();
                modelo.EstadisticasAgrupadas = new List<IGrouping<dynamic, EstadisticasUsuarios>>();
            }

            return View(modelo);
        }

        [HttpGet]
        [Monitoreo("SistemasPorNegocio", "SELECT", "obtenerSistemasPorNegocio")]
        public async Task<JsonResult> SistemasPorNegocio(int? idNegocio)
        {
            var sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);
            return Json(sistemas);
        }
    }
}