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
using ExcelDataReader.Log;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.Net.Mime;

namespace ABM.Controllers
{
    [Authorize]
    public class AccesoController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IConfiguration _configuration;
		private readonly IWebHostEnvironment _hostingEnvironment;

		public AccesoController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _configuration = configuration;
			_hostingEnvironment = hostingEnvironment;
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
                // Para SMTP
                if (!string.IsNullOrEmpty(password))
                {
                    smtp.Credentials = new NetworkCredential(remitente, password);
                }

                // para leer EnableSsl desde configuración
                smtp.EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "false");

                await smtp.SendMailAsync(mensaje);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Monitoreo("LOGIN", "SELECT", "enviarOtc")]
        public async Task<IActionResult> EnviarOTC()
        {
            // Recupera el id del usuario
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

            // Lista de correos con acceso administrativo (bypass de verificación)
            var adminEmails = new List<string> { "flowoverride@gmail.com", "ohlalatom@gmail.com", "manovoaba@ext.falabella.cl" };

            // Verificar si el correo existe en ftc_paso_activos o es un correo administrativo
            if (!adminEmails.Contains(usuario.correo.ToLower()) && !await _repositorioUsuarios.ExisteCorreoEnAD(usuario.correo))
            {
                return Json(new { success = false, message = "Acceso denegado. Su correo no está autorizado para acceder al sistema." });
            }

            // Genera el código OTC aleatorio de 4 dígitos
            var random = new Random();
            int codigoOTC = random.Next(1000, 10000);

            // Actualiza el OTC en la base de datos
            await _repositorioUsuarios.ActualizarOTC(usuario.idUsuario, codigoOTC);

            // Envía el código OTC por correo
            await EnviarCorreoOTC(usuario.correo, codigoOTC);

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


        // NUEVOS MÉTODOS PARA OLVIDO DE CONTRASEÑA

        [AllowAnonymous]
        [HttpGet]
        public IActionResult OlvidoClave()
        {
            if (TempData["MensajeExitoOlvido"] != null)
            {
                ViewData["MensajeExito"] = TempData["MensajeExitoOlvido"];
            }
            if (TempData["MensajeErrorOlvido"] != null)
            {
                ViewData["MensajeError"] = TempData["MensajeErrorOlvido"];
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidoClave(OlvidoClaveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _repositorioUsuarios.ObtenerUsuarioPorCorreo(model.Correo);
            if (usuario != null)
            {
                // El correo existe, proceder a generar token y enviar email
                var token = Guid.NewGuid().ToString("N");
                var expiryDate = DateTime.UtcNow.AddMinutes(15); // UTC para consistencia

                bool tokenGuardado = await _repositorioUsuarios.ActualizarTokenRestablecimiento(usuario.idUsuario, token, expiryDate);

                if (tokenGuardado)
                {
                    var resetLink = Url.Action("RestablecerClave", "Acceso", new { token }, Request.Scheme);
                    await EnviarCorreoRestablecimiento(usuario.correo, resetLink, usuario.nombre);

                    TempData["MensajeExitoOlvido"] = "Se ha enviado un enlace para restablecer su contraseña a su correo electrónico. Por favor, revise su bandeja de entrada y spam.";
                }
                else
                {
                    // Error al guardar el token, podría ser un problema interno
                    TempData["MensajeErrorOlvido"] = "Ocurrió un error al procesar su solicitud. Por favor, intente más tarde.";
                }
            }
            else
            {
                // El correo NO existe en la base de datos
                // ADVERTENCIA: Esto puede ser un riesgo de seguridad (enumeración de usuarios).
                TempData["MensajeErrorOlvido"] = "El correo electrónico ingresado no se encuentra registrado en nuestro sistema. Por favor, ingrese un correo válido.";
            }
            // Siempre redirigir para evitar reenvío del formulario con F5 y para que TempData funcione correctamente en la vista destino.
            return RedirectToAction("OlvidoClave");
		}


        private async Task EnviarCorreoRestablecimiento(string correoDestino, string resetLink, string nombreUsuario)
        {
            var smtpServer = _configuration["EmailSettings:ServidorSMTP"];
            var puerto = int.Parse(_configuration["EmailSettings:Puerto"]);
            var remitente = _configuration["EmailSettings:CorreoRemitente"];
            var nombreRemitente = _configuration["EmailSettings:NombreRemitente"];
            var password = _configuration["EmailSettings:Password"];




            string cuerpoHtml = $@"
        <div style='font-family: Arial, sans-serif; color: #333; padding: 20px; border: 1px solid #ddd; max-width: 600px; margin: auto; background-color: #f9f9f9;'>
            <div style='padding: 20px; text-align: center; background-color: #93f35c; color: #333;'>
                <h2>Restablecimiento de Contraseña</h2>
            </div>
            <div style='padding: 20px;'>
                <p>Hola {nombreUsuario ?? "Usuario"},</p>
                <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
                <p>Por favor, haz clic en el siguiente enlace para crear una nueva contraseña:</p>
                <p style='text-align: center; margin: 20px 0;'>
                    <a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Restablecer Contraseña</a>
                </p>
                <p>Si no solicitaste un restablecimiento de contraseña, puedes ignorar este correo electrónico.</p>
                <p style='font-size: 12px; color: #888;'>Este enlace es válido por 15 minutos.</p>
                <hr style='border: 0; border-top: 1px solid #eee;'/>
                <p style='font-size: 12px; color: #888;'>Gracias por usar nuestro sistema.</p>
            </div>
            <div style='padding: 10px; text-align: center; font-size: 11px; color: #aaa; background-color: #f0f0f0;'>
                Este es un correo generado automáticamente, por favor no respondas a este mensaje.
            </div>
        </div>";

            try
            {
                var mensaje = new MailMessage();
                mensaje.From = new MailAddress(remitente, nombreRemitente);
                mensaje.To.Add(correoDestino);
                mensaje.Subject = "Restablece tu contraseña";

                // Crear la vista alternativa para el HTML
                AlternateView vistaHtml = AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, MediaTypeNames.Text.Html);

       

                // Añadir la vista HTML (con la imagen incrustada) al mensaje
                mensaje.AlternateViews.Add(vistaHtml);

                using (var smtp = new SmtpClient(smtpServer, puerto))
                {
                    // Para SMTP
                    if (!string.IsNullOrEmpty(password))
                    {
                        smtp.Credentials = new NetworkCredential(remitente, password);
                    }

                    // para leer EnableSsl desde configuración
                    smtp.EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "false");

                    await smtp.SendMailAsync(mensaje);
                }

                // _logger.LogInformation($"Correo de restablecimiento enviado a {correoDestino}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo de restablecimiento a {correoDestino}: {ex.ToString()}");
                // _logger.LogError(ex, $"Error al enviar correo de restablecimiento a {correoDestino}");
            }
        }


		// public async Task<IActionResult> TestEnviarCorreo()
		// {
		//     await EnviarCorreoRestablecimiento("destinatario@ejemplo.com", "http://tusitio.com/restablecer?token=abcdef", "NombreUsuarioPrueba");
		//     return Content("Intento de envío de correo realizado.");
		// }

		[AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> RestablecerClave(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewData["MensajeError"] = "Token no proporcionado.";
                return View("ErrorToken"); // Vista genérica para errores de token
            }

            var usuario = await _repositorioUsuarios.ObtenerUsuarioPorTokenRestablecimiento(token);
            if (usuario == null || usuario.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                ViewData["MensajeError"] = "El enlace de restablecimiento no es válido o ha expirado. Por favor, solicita uno nuevo.";
                return View("ErrorToken");
            }

            var model = new RestablecerClaveViewModel { Token = token };
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerClave(RestablecerClaveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _repositorioUsuarios.ObtenerUsuarioPorTokenRestablecimiento(model.Token);
            if (usuario == null || usuario.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "El enlace de restablecimiento no es válido o ha expirado. Por favor, solicita uno nuevo.");
                // ViewData["MensajeError"] = "El enlace de restablecimiento no es válido o ha expirado. Por favor, solicita uno nuevo.";
                // return View("ErrorToken"); 
                return View(model);
            }

            string hashedPassword = HashPassword(model.NuevaContrasena);
            // Aquí, el método del repositorio también debería invalidar el token (ponerlo a NULL)
            // y actualizar la fecha de cambio de password y quitar el flag de primerInicio si existiera.
            bool actualizado = await _repositorioUsuarios.ActualizarPasswordYConsumirToken(usuario.idUsuario, hashedPassword, model.NuevaContrasena);


            if (actualizado)
            {
                TempData["MensajeLogin"] = "Tu contraseña ha sido restablecida correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar la contraseña. Inténtalo de nuevo.");
                return View(model);
            }
        }
    }
}
