using Microsoft.AspNetCore.Authorization;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Mail;
using System.Net;

namespace ABM.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IConfiguration _configuration;

        public RolesController(IRepositorioRoles repositorioRoles, IConfiguration configuration)
        {
            _repositorioRoles = repositorioRoles;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> ListarRoles()
        {
            var rolesConPNS = await _repositorioRoles.ObtenerRolesConPNS();
            return View(rolesConPNS);
        }

    }
}
