using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ABM.Controllers
{
    public class AccesoController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;

        public AccesoController(IRepositorioUsuarios repositorioUsuarios)
        {
            _repositorioUsuarios = repositorioUsuarios;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _repositorioUsuarios.ValidarUsuario(model.Correo, model.Repeat_Password);

            if (usuario != null)
            {
                // Configuración de claims para el usuario autenticado
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.nombre),
                    // Se pueden agregar más claims según la necesidad
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Actualizar el campo FultimoAcceso u otros campos, si es necesario

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Credenciales inválidas.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}
