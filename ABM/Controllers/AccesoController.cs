using Microsoft.AspNetCore.Authorization;
using ABM.Models;
using ABM.Filters;
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
    public class AccesoController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IConfiguration _configuration;

        public AccesoController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IConfiguration configuration)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [Monitoreo("LOGIN", "VALIDATE", "validarUsuario")]
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

                // Cargar la información de países y negocios según el rol
                var rolesConPNS = await _repositorioRoles.ObtenerRolesConPNS();
                var rolConPNS = rolesConPNS.FirstOrDefault(r => r.idRol == usuario.idRol);
                if (rolConPNS != null)
                {
                    ViewBag.PaisesNegocios = rolConPNS.PaisesNegocios; // List< PaisNegocioViewModel >
                }

                // Crear la sesión sin enviar el OTC todavía
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

                // Indicador para que se muestre el modal en la vista
                ViewBag.PedirOTC = true;
                ViewBag.IdUsuario = usuario.idUsuario;

                // Guardamos el id en TempData para poder usarlo en la acción EnviarOTC (opcionalmente también en Session)
                TempData["idUsuario"] = usuario.idUsuario;

                return View(model);
            }
            else
            {
                ModelState.AddModelError("", "Credenciales inválidas.");
                return View(model);
            }
        }


        private async Task EnviarCorreoOTC(string correoDestino, int codigo)
        {
            var smtpServer = _configuration["EmailSettings:ServidorSMTP"];
            var puerto = int.Parse(_configuration["EmailSettings:Puerto"]);
            var remitente = _configuration["EmailSettings:CorreoRemitente"];
            var nombreRemitente = _configuration["EmailSettings:NombreRemitente"];
            var password = _configuration["EmailSettings:Password"];

            // HTML del correo
            string html = $@"
        <div style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
            <h2 style='color: #4CAF50;'>Código de verificación</h2>
            <p>Hola,</p>
            <p>Tu código de verificación para ingresar al sistema es:</p>
            <p style='font-size: 24px; font-weight: bold; color: #4CAF50;'>{codigo}</p>
            <hr />
            <p style='font-size: 12px; color: #888;'>Este código es válido por un tiempo limitado. No lo compartas con nadie.</p>
            <p style='font-size: 12px;'>Gracias por usar nuestro sistema.</p>
        </div>
    ";

            var mensaje = new MailMessage();
            mensaje.From = new MailAddress(remitente, nombreRemitente);
            mensaje.To.Add(correoDestino);
            mensaje.Subject = "Tu código de verificación OTC";
            mensaje.Body = html;
            mensaje.IsBodyHtml = true;

            using (var smtp = new SmtpClient(smtpServer, puerto))
            {
                smtp.Credentials = new NetworkCredential(remitente, password);
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(mensaje);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Monitoreo("LOGIN", "SELECT", "enviarOtc")]
        public async Task<IActionResult> EnviarOTC()
        {
            // Recupera el id del usuario (puedes obtenerlo desde TempData, Session o un parámetro seguro)
            if (TempData["idUsuario"] == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado." });
            }

            int idUsuario = Convert.ToInt32(TempData["idUsuario"]);
            var usuario = await _repositorioUsuarios.ObtenerPorId(idUsuario);
            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado." });
            }

            // Genera el código OTC aleatorio de 4 dígitos
            var random = new Random();
            int codigoOTC = random.Next(1000, 10000);

            // Actualiza el OTC en la base de datos
            await _repositorioUsuarios.ActualizarOTC(usuario.idUsuario, codigoOTC);

            // Envía el código OTC por correo
            await EnviarCorreoOTC(usuario.correo, codigoOTC);

            // Opcional: Reestablece TempData["idUsuario"] si lo necesitas en otros flujos
            TempData["idUsuario"] = usuario.idUsuario;

            return Json(new { success = true });
        }


        [AllowAnonymous]
        [HttpGet]
        [Monitoreo("LOGIN", "VALIDATE", "validarOtc")]
        public async Task<IActionResult> ValidarOTC(int idUsuario, string otcIngresado, int idPais, string pais, int idNegocio, string negocio)
        {
            var usuario = await _repositorioUsuarios.ObtenerPorId(idUsuario);
            if (usuario == null)
                return Json(new { valido = false });

            bool esValido = usuario.otc.ToString() == otcIngresado;

            if (esValido)
            {
                // Guardar los valores de país y negocio en la sesión
                HttpContext.Session.SetString("Pais", pais);
                HttpContext.Session.SetString("Negocio", negocio);
                HttpContext.Session.SetInt32("IdPais", idPais);
                HttpContext.Session.SetInt32("IdNegocio", idNegocio);
            }

            return Json(new { valido = esValido });
        }


        [HttpPost]
        [Monitoreo("LOGOUT", "SELECT", "logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }       


        [AllowAnonymous]
        [HttpGet]
        [Monitoreo("CambiarPasswordPrimerInicio", "SELECT", "verFormularioCambioPasswordPrimerInicio")]
        public IActionResult CambiarPasswordPrimerInicio()
        {
            if (TempData["idUsuario"] == null)
            {
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }

            // Convertir el valor de TempData a int y asignarlo al modelo
            int idUsuario = Convert.ToInt32(TempData["idUsuario"]);
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
        [Monitoreo("CambiarPasswordPrimerInicio", "UPDATE", "cambiarPasswordPrimerInicio")]
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
