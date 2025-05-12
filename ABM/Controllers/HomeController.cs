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
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;
        private readonly IRepositorioRoles repositorioRoles;

        public HomeController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
            repositorioRoles = RepositorioRoles;
        }

        [HttpGet]
        [Monitoreo("Index", "SELECT", "verIndex")]
        public IActionResult Index()
        {
            return View();
        }


        // 1. Acción GET que devuelve la lista para el modal
        [HttpGet]
        [Monitoreo("PnsSelectorPartial", "SELECT", "verPnsSelectorPartial")]
        public async Task<IActionResult> PnsSelectorPartial()
        {
            // 1) Datos del usuario logueado
            var usuario = await repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            if (usuario == null)
                return Content("");

            // 2) Todos los roles con PNS y filtramos por el rol del usuario
            var rolesConPNS = await repositorioRoles.ObtenerRolesConPNS();
            var rolConPNS = rolesConPNS.FirstOrDefault(r => r.idRol == usuario.idRol);

            // Si sólo tiene un par país-negocio, no mostramos nada
            if (rolConPNS == null || rolConPNS.PaisesNegocios.Count <= 1)
                return Content("");

            // 3) Devolvemos la vista parcial
            return PartialView("_PnsSelectorPartial", rolConPNS.PaisesNegocios);
        }

		// 2. Acción POST que recibe la selección y actualiza la sesión
		[Authorize]
		[HttpPost]
        [Monitoreo("CambiarPns", "SELECT", "cambiarPns")]
        public IActionResult CambiarPns([FromBody] CambiarPnsRequest req)
		{
			// req.Pais ya no es null
			HttpContext.Session.SetInt32("IdPais", req.IdPais);
			HttpContext.Session.SetString("Pais", req.Pais);
			HttpContext.Session.SetInt32("IdNegocio", req.IdNegocio);
			HttpContext.Session.SetString("Negocio", req.Negocio);

			return Json(new { success = true });
		}


        [HttpGet]
        [Monitoreo("Privacy", "SELECT", "verPrivacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Monitoreo("Salir", "SELECT", "salirSistema")]
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





    }
}
