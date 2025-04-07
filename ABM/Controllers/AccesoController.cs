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

        [HttpGet]
        public IActionResult Registrarse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrarse(RegistroUsuarioVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Password != model.Repeat_Password)
            {
                ModelState.AddModelError("Repeat_Password", "Las contraseñas no coinciden.");
                return View(model);
            }

            if (await _repositorioUsuarios.ExisteCorreo(model.Correo))
            {
                ModelState.AddModelError("Correo", "El correo ya se encuentra registrado.");
                return View(model);
            }

            if (await _repositorioUsuarios.ExisteUsuario(model.Usuario))
            {
                ModelState.AddModelError("Usuario", "El nombre de usuario ya está en uso.");
                return View(model);
            }

            try
            {
                var nuevoUsuario = new Usuario
                {
                    nombre = model.Nombre,
                    apellidos = model.Apellidos,
                    rut = model.Rut,
                    telefono = model.Telefono,
                    correo = model.Correo,
                    usuario = model.Usuario,
                    Fcreacion = DateTime.Now,
                    otc = new Random().Next(1000, 10000).ToString(),
                    inicioOtc = DateTime.Now,
                    MesesExpiracionClave = model.MesesExpiracionClave,
                    estado = "1",
                    password = HashPassword(model.Password),
                    repeat_password = model.Repeat_Password
                };

                int idNuevoUsuario = await _repositorioUsuarios.RegistrarUsuario(nuevoUsuario);

                // Redireccionar si todo va bien
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Puedes loguearlo si tienes sistema de logs
                ModelState.AddModelError("", "Ocurrió un error al registrar el usuario. Intente nuevamente.");
                return View(model);
            }
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
