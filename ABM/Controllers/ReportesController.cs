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
        public async Task<IActionResult> Finiquitados()
        {
            // leer con las mismas claves que usaste al guardar
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            var lista = await repositorioReportes
                 .ObtenerListaFiniquitadosPorSistema(idPais, idNegocio);

            return View(lista);
        }


    }
}
