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
    public class ReportesController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;
        private readonly IRepositorioRoles repositorioRoles;
        private readonly IRepositorioReportes repositorioReportes;

        public ReportesController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles, IRepositorioReportes RepositorioReportes)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
            repositorioRoles = RepositorioRoles;
            repositorioReportes = RepositorioReportes;
        }


        [HttpGet]
        [Monitoreo("Finiquitados", "SELECT", "verFiniquitadosPorSistema")]
        public async Task<IActionResult> Finiquitados(string sistema = null)
        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            // 1) traigo todos los finiquitados
            var lista = (await repositorioReportes
                .ObtenerListaFiniquitadosPorSistema(idPais, idNegocio))
                .ToList();

            // 2) extraigo sistemas únicos para el dropdown
            var sistemas = lista
                .Select(x => x.sistema)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ViewBag.Sistemas = sistemas;
            ViewBag.SistemaSeleccionado = sistema;

            // 3) si viene filter, aplico en memoria
            if (!string.IsNullOrEmpty(sistema))
                lista = lista.Where(x => x.sistema == sistema).ToList();

            return View(lista);
        }


        [HttpGet]
        [Monitoreo("UsuariosNoEncontrados", "SELECT", "verUsuariosNoEncontrados")]
        public async Task<IActionResult> UsuariosNoEncontrados(string sistema = null)
        {
            // 1) Leer sesión
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            // 2) Traer TODOS los “NoEncontrados”
            var lista = (await repositorioReportes
                .ObtenerListaUsuariosNoEncontrados(idPais, idNegocio))
                .ToList();

            // 3) Extraer sistemas únicos para el dropdown
            var sistemas = lista
                .Select(x => x.sistema)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ViewBag.Sistemas = sistemas;
            ViewBag.SistemaSeleccionado = sistema;

            // 4) Filtrar en memoria si viene parámetro
            if (!string.IsNullOrEmpty(sistema))
                lista = lista.Where(x => x.sistema == sistema).ToList();

            return View(lista);
        }


        [HttpGet]
        [Monitoreo("UsuariosActivos", "SELECT", "verUsuariosActivos")]
        public async Task<IActionResult> UsuariosActivos(string sistema = null)
        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            // 1) traer todos los activos
            var lista = (await repositorioReportes
                .ObtenerListaUsuariosActivos(idPais, idNegocio))
                .ToList();

            // 2) sistemas únicos para el dropdown
            var sistemas = lista
                .Select(x => x.sistema)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ViewBag.Sistemas = sistemas;
            ViewBag.SistemaSeleccionado = sistema;

            // 3) filtrar en memoria si se pidió
            if (!string.IsNullOrEmpty(sistema))
                lista = lista.Where(x => x.sistema == sistema).ToList();

            return View(lista);
        }



        [HttpGet]
        [Monitoreo("BuscarUsuarios", "SELECT", "buscarUsuariosPorNombreORut")]
        public async Task<IActionResult> BuscarUsuarios(string rutDni = null, string nombreUsuario = null)
        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");
            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            // mantengo los valores para el formulario
            ViewBag.RutDni = rutDni;
            ViewBag.NombreUsuario = nombreUsuario;

            List<UsersBuscar> lista;

            // sólo llamo al repositorio si hay al menos un criterio
            if (string.IsNullOrWhiteSpace(rutDni) && string.IsNullOrWhiteSpace(nombreUsuario))
            {
                lista = new List<UsersBuscar>();
            }
            else
            {
                lista = (await repositorioReportes
                    .ObtenerUsuariosPorRutONombre(idPais, idNegocio, rutDni, nombreUsuario))
                    .ToList();
            }

            return View(lista);
        }



    }
}
