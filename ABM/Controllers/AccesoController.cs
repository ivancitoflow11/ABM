using Microsoft.AspNetCore.Authorization;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ABM.Controllers
{
    [Authorize]
    public class AccesoController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;

        public AccesoController(IRepositorioUsuarios repositorioUsuarios)
        {
            _repositorioUsuarios = repositorioUsuarios;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
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
                // Si es el primer inicio, redirigimos a cambiar contraseña
                if (usuario.primerInicio)
                {
                    TempData["idUsuario"] = usuario.idUsuario;
                    return RedirectToAction("CambiarPasswordPrimerInicio", "Acceso");
                }
                if (usuario.FechaCambioPassword != null)
                {
                    var fechaExpiracion = usuario.FechaCambioPassword.Value.AddMonths(usuario.MesesExpiracionClave);
                    if (DateTime.Now >= fechaExpiracion)
                    {
                        TempData["idUsuario"] = usuario.idUsuario;
                        TempData["ExpiracionClave"] = true;
                        return RedirectToAction("CambiarPasswordPrimerInicio", "Acceso");
                    }
                }


                // Si todo bien, iniciar sesión
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.nombre),
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                ViewBag.PedirOTC = true;
                ViewBag.IdUsuario = usuario.idUsuario;
                return View(model);

            }
            else
            {
                ModelState.AddModelError("", "Credenciales inválidas.");
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ValidarOTC(int idUsuario, string otcIngresado)
        {
            var usuario = await _repositorioUsuarios.ObtenerPorId(idUsuario);

            if (usuario == null)
                return Json(new { valido = false });

            bool esValido = usuario.otc.ToString() == otcIngresado;
            return Json(new { valido = esValido });
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public async Task<IActionResult> Registrarse()
        {
            var model = new RegistroUsuarioVM
            {
                RolesDisponibles = await _repositorioUsuarios.ObtenerRoles()
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrarse(RegistroUsuarioVM model)
        {
            if (!ModelState.IsValid)
            {
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            if (model.Password != model.Repeat_Password)
            {
                ModelState.AddModelError("Repeat_Password", "Las contraseñas no coinciden.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            if (await _repositorioUsuarios.ExisteCorreo(model.Correo))
            {
                ModelState.AddModelError("Correo", "El correo ya se encuentra registrado.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            if (await _repositorioUsuarios.ExisteUsuario(model.Usuario))
            {
                ModelState.AddModelError("Usuario", "El nombre de usuario ya está en uso.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
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
                    repeat_password = model.Repeat_Password,
                    idRol = model.RolId
                };

                int idNuevoUsuario = await _repositorioUsuarios.RegistrarUsuario(nuevoUsuario);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al registrar el usuario. Intente nuevamente.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult CambiarPasswordPrimerInicio()
        {
            // Se asume que en el login se almacenó el Id del usuario en TempData, por ejemplo TempData["idUsuario"]
            if (TempData["idUsuario"] == null)
            {
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }

            // Convertir el valor de TempData a int y asignarlo al modelo
            int idUsuario = Convert.ToInt32(TempData["idUsuario"]);
            // Si deseas conservar TempData para el post, puedes reasignarlo
            TempData.Keep("idUsuario");

            var model = new CambioPasswordVM
            {
                IdUsuario = idUsuario
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPasswordPrimerInicio(CambioPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IdUsuario <= 0)
            {
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }

            // Calcula el hash de la nueva contraseña.
            string hashedPassword = HashPassword(model.NuevaContrasena);

            // Se llama al método actualizado del repositorio enviando tanto el hash como la contraseña original
            bool actualizado = await _repositorioUsuarios.ActualizarPasswordPrimerInicio(model.IdUsuario, hashedPassword, model.NuevaContrasena);

            if (actualizado)
            {
                TempData["Mensaje"] = "Contraseña actualizada correctamente. Por favor, inicie sesión.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar la contraseña. Intente nuevamente.");
                return View(model);
            }
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
