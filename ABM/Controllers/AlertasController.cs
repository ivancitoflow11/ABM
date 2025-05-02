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

		public async Task<IActionResult> AlertaSistema()
		{
			AlertaSistemaViewModel modelo = new AlertaSistemaViewModel();

			modelo.estadisticas = await repositorioAlertas.ObtenerEstadisticasUsuarios();
			//En estas querys hay que hacer que no se repitan rutdni
			modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados();
			modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados();
			modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados();

			return View(modelo);
		}
	}
}
