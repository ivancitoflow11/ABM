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
        public async Task<IActionResult> AlertaSistema(int? idPais, int? idNegocio)
        {
            AlertaSistemaViewModel modelo = new AlertaSistemaViewModel();

            modelo.estadisticas = await repositorioAlertas.ObtenerEstadisticasUsuarios(idPais, idNegocio);
            modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados(idPais, idNegocio);
            modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados(idPais, idNegocio);
            modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados(idPais, idNegocio);

            // Estas líneas aseguran que tus filtros permanezcan visibles después del filtrado.
            var usuario = await repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            var rolConPNS = (await repositorioRoles.ObtenerRolesConPNS())
                            .FirstOrDefault(r => r.idRol == usuario.idRol);

            ViewBag.PaisesNegocios = rolConPNS?.PaisesNegocios ?? new List<PaisNegocioViewModel>();

            ViewBag.IdPaisSeleccionado = idPais;
            ViewBag.IdNegocioSeleccionado = idNegocio;

            return View(modelo);
        }


    }
}
