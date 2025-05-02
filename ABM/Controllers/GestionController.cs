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
    public class GestionController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;
        private readonly IRepositorioRoles repositorioRoles;
        private readonly IRepositorioGestion repositorioGestion;

        public GestionController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles, IRepositorioGestion RepositorioGestion)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
            repositorioRoles = RepositorioRoles;
            repositorioGestion = RepositorioGestion;
        }

        public async Task<IActionResult> Gestion()
        {
            // Leer valores de sesión
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
                return RedirectToAction("PnsSelectorPartial", "Home");

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            ResumenGestionViewModel modelo2 = await repositorioGestion.ObtenerDatosGestion(idPais, idNegocio);
            var modelo = mapper.Map<GestionViewModel>(modelo2);

            modelo.ListaCasosCargo = await repositorioGestion.ObtenerListaCasosCargo(idPais, idNegocio);
            modelo.ListaEvidenciasFiniquitado = await repositorioGestion.ObtenerListaEvidenciasFiniquitado(idPais, idNegocio);
            modelo.ListaResumenPais = await repositorioGestion.ObtenerListaResumenPais(idPais, idNegocio);
            modelo.ListaTendenciaDiaria = await repositorioGestion.ObtenerListaTendenciaDiaria(idPais, idNegocio);

            return View(modelo);
        }

    }
}
