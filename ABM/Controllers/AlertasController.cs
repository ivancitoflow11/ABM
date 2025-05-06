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
        public async Task<IActionResult> AlertaSistema(int? idNegocio, int? idSistema)
        {
            var modelo = new AlertaSistemaViewModel();

            // 1) Tus datos de alerta
            modelo.estadisticas = await repositorioAlertas.ObtenerEstadisticasUsuarios(idNegocio, idSistema);
            modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados(idNegocio, idSistema);
            modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados(idNegocio, idSistema);
            modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados(idNegocio, idSistema);

            // 2) Obtengo los PaisesNegocios desde tu repositorio de roles
            var usuario = await repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            var rolConPNS = (await repositorioRoles.ObtenerRolesConPNS())
                            .FirstOrDefault(r => r.idRol == usuario.idRol);

            // 3) Extraigo sólo los Negocios (distinct por idNegocio),
            //    evitando el uso de '??' entre tipos incompatibles
            List<PaisNegocioViewModel> listaNegocios = new();
            if (rolConPNS?.PaisesNegocios != null)
            {
                listaNegocios = rolConPNS.PaisesNegocios
                    .GroupBy(x => x.idNegocio)
                    .Select(g => g.First())
                    .ToList();
            }
            ViewBag.Negocios = listaNegocios;

            // 4) Traigo los sistemas según el negocio seleccionado
            ViewBag.Sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);

            // 5) Para mantener la opción seleccionada tras el submit
            ViewBag.IdNegocioSeleccionado = idNegocio;
            ViewBag.IdSistemaSeleccionado = idSistema;

            return View(modelo);
        }

        [HttpGet]
        public async Task<JsonResult> SistemasPorNegocio(int? idNegocio)
        {
            // Llama al mismo método que ya tienes en el repositorio
            var sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);
            return Json(sistemas);
        }





    }
}
