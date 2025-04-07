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
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;

        public HomeController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

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
