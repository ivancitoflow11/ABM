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

            // 1) Aseguramos que el mapeo base nunca sea null
            var resumenDto = await repositorioGestion.ObtenerDatosGestion(idPais, idNegocio)
                             ?? new ResumenGestionViewModel();

            // 2) Mapear y garantizar que el ViewModel no quede null
            var modelo = mapper.Map<GestionViewModel>(resumenDto)
                         ?? new GestionViewModel();

            // 3) Cada lista coalesceada a lista vacía si el repositorio devolvió null
            modelo.ListaCasosCargo = await repositorioGestion.ObtenerListaCasosCargo(idPais, idNegocio)
                                                   ?? Enumerable.Empty<CasosCargoViewModel>();
            modelo.ListaEvidenciasFiniquitado = await repositorioGestion.ObtenerListaEvidenciasFiniquitado(idPais, idNegocio)
                                                   ?? Enumerable.Empty<EvidenciasFiniquitadoViewModel>();
            modelo.ListaResumenPais = await repositorioGestion.ObtenerListaResumenPais(idPais, idNegocio)
                                                   ?? Enumerable.Empty<ResumenPaisViewModel>();
            modelo.ListaTendenciaDiaria = await repositorioGestion.ObtenerListaTendenciaDiaria(idPais, idNegocio)
                                                   ?? Enumerable.Empty<TendenciaDiariaViewModel>();

            return View(modelo);
        }


    }
}
