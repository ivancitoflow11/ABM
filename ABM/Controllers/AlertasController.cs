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
        public async Task<IActionResult> AlertaSistema(int? idNegocio, int? idSistema)
        {
            // Se instancia el ViewModel actualizado.
            var modelo = new AlertaSistemaViewModel();

            // 1. OBTENER DATOS BASE
            // Se obtienen todas las estadísticas de una sola vez.
            var estadisticasCompletas = await repositorioAlertas.ObtenerEstadisticasUsuarios(idNegocio, idSistema);

            // 2. RELLENAR PROPIEDADES DEL VIEWMODEL
            // a) Se asigna la lista completa para mantener la compatibilidad con los modales.
            modelo.estadisticas = estadisticasCompletas;
            modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados(idNegocio, idSistema);
            modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados(idNegocio, idSistema);
            modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados(idNegocio, idSistema);

            // b) Se crea la lista de países para la cabecera de la tabla.
            //    Esto toma los países únicos de los datos obtenidos para no mostrar columnas vacías.
            //    IMPORTANTE: Esto asume que tu modelo 'EstadisticasUsuarios' contiene la propiedad 'Bandera'.
            //    Si 'Bandera' no está en 'EstadisticasUsuarios', deberás obtenerla de tu tabla [ftc_pais].
            modelo.PaisesEnCabecera = estadisticasCompletas
                .Select(e => new PaisViewModel { pais = e.Pais, Bandera = e.Bandera })
                .GroupBy(p => p.pais)
                .Select(g => g.First())
                .OrderBy(p => p.pais) // Se ordena para que las columnas siempre aparezcan en el mismo orden.
                .ToList();

            // c) Se agrupan las estadísticas por Negocio y Sistema para las filas de la tabla.
            modelo.EstadisticasAgrupadas = estadisticasCompletas
                .GroupBy(e => new { e.Negocio, e.Sistema })
                .ToList();

            // 3. LÓGICA PARA FILTROS (Sin cambios)
            // Se obtienen los negocios a los que el usuario tiene acceso.
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

            // Se traen los sistemas según el negocio seleccionado.
            ViewBag.Sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);

            // Se mantienen las opciones seleccionadas en los filtros después del submit.
            ViewBag.IdNegocioSeleccionado = idNegocio;
            ViewBag.IdSistemaSeleccionado = idSistema;

            // 4. DEVOLVER LA VISTA CON EL MODELO COMPLETO
            return View(modelo);
        }

        [HttpGet]
        [Monitoreo("SistemasPorNegocio", "SELECT", "obtenerSistemasPorNegocio")]
        public async Task<JsonResult> SistemasPorNegocio(int? idNegocio)
        {
            // Llama al mismo método que ya tienes en el repositorio
            var sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);
            return Json(sistemas);
        }





    }
}
