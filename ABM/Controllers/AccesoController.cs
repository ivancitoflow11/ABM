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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ABM.Controllers
{
    [Authorize]
    public class AccesoController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IConfiguration _configuration;
		private readonly IWebHostEnvironment _hostingEnvironment;

        // Contexto de "cambio de clave pendiente": sólo lo emite Login() tras validar la contraseña
        // (primer inicio o clave expirada). El usuario a modificar sale de acá, nunca del cuerpo del POST.
        private const string TempDataCambioClave = "idUsuarioCambioClave";

        // Segundo factor (OTC). Tras validar la contraseña NO se emite la cookie de autenticación: sólo queda un
        // "ticket parcial" en la Session del servidor, que habilita únicamente EnviarOTC/ValidarOTC. La cookie real
        // se emite en ValidarOTC, cuando el código se verifica en el servidor.
        private const string SesionMfaUsuario = "mfa_idUsuario";
        private const string SesionMfaInicio = "mfa_inicio";
        private const int LongitudOtc = 6;
        private const int MaxFallosPorUsuario = 5;
        private const int MaxFallosPorIp = 20;
        private const string MensajeBloqueo = "Demasiados intentos fallidos. Espere unos minutos e inicie sesión nuevamente para solicitar un nuevo código.";
        private static readonly TimeSpan VigenciaOtc = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VigenciaMfaPendiente = TimeSpan.FromMinutes(10);

        private readonly IOtcIntentosService _otcIntentos;

		public AccesoController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IOtcIntentosService otcIntentos)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _configuration = configuration;
			_hostingEnvironment = hostingEnvironment;
            _otcIntentos = otcIntentos;
		}

        private void IniciarMfaPendiente(int idUsuario)
        {
            HttpContext.Session.SetInt32(SesionMfaUsuario, idUsuario);
            HttpContext.Session.SetString(SesionMfaInicio, DateTime.UtcNow.Ticks.ToString());
        }

        private void LimpiarMfaPendiente()
        {
            HttpContext.Session.Remove(SesionMfaUsuario);
            HttpContext.Session.Remove(SesionMfaInicio);
        }

        private int? ObtenerMfaPendiente()
        {
            var idUsuario = HttpContext.Session.GetInt32(SesionMfaUsuario);
            if (idUsuario == null || !long.TryParse(HttpContext.Session.GetString(SesionMfaInicio), out var ticks))
            {
                return null;
            }

            if (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > VigenciaMfaPendiente)
            {
                LimpiarMfaPendiente();
                return null;
            }

            return idUsuario;
        }

        private string ObtenerIpCliente() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        private async Task<List<PaisNegocioViewModel>> ObtenerPaisesNegociosDelRolAsync(int? idRol)
        {
            var roles = await _repositorioRoles.ObtenerRolesConPNS();
            return roles.FirstOrDefault(r => r.idRol == idRol)?.PaisesNegocios ?? new List<PaisNegocioViewModel>();
        }

        private static bool CodigoOtcCoincide(string otcGuardado, string otcIngresado)
        {
            if (string.IsNullOrWhiteSpace(otcGuardado) || string.IsNullOrEmpty(otcIngresado)
                || !Regex.IsMatch(otcIngresado, $@"^\d{{{LongitudOtc}}}$"))
            {
                return false;
            }

            var esperado = Encoding.UTF8.GetBytes(otcGuardado.Trim());
            var recibido = Encoding.UTF8.GetBytes(otcIngresado);
            return esperado.Length == recibido.Length && CryptographicOperations.FixedTimeEquals(esperado, recibido);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // Con la sesión ya iniciada (OTC validado y país/negocio guardados en el servidor) no se vuelve a mostrar
            // el formulario: se lleva al Home. Si la cookie sigue viva pero la sesión del servidor se perdió (por
            // ejemplo, tras reciclar el sitio), se deja ver el login para que la persona pueda entrar de nuevo.
            if (User.Identity?.IsAuthenticated == true
                && HttpContext.Session.GetInt32("IdPais") != null
                && HttpContext.Session.GetInt32("IdNegocio") != null)
            {
                return RedirectToAction("Index", "Home");
            }

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
                    TempData[TempDataCambioClave] = usuario.idUsuario;
                    return RedirectToAction("CambiarPasswordPrimerInicio", "Acceso");
                }

                if (usuario.FechaCambioPassword != null)
                {
                    var fechaExpiracion = usuario.FechaCambioPassword.Value.AddMonths(usuario.MesesExpiracionClave);
                    if (DateTime.Now >= fechaExpiracion)
                    {
                        TempData[TempDataCambioClave] = usuario.idUsuario;
                        TempData["ExpiracionClave"] = true;
                        return RedirectToAction("CambiarPasswordPrimerInicio", "Acceso");
                    }
                }

                // Países y negocios que el rol puede elegir (el modal los muestra; ValidarOTC los vuelve a verificar).
                ViewBag.PaisesNegocios = await ObtenerPaisesNegociosDelRolAsync(usuario.idRol);

                // Contraseña correcta, pero todavía NO hay sesión: se descarta cualquier sesión previa y se deja
                // un ticket parcial en el servidor. La cookie de autenticación se emite recién al validar el OTC.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                IniciarMfaPendiente(usuario.idUsuario);

                // Indicador para que se muestre el modal en la vista
                ViewBag.PedirOTC = true;
                ViewBag.LongitudOtc = LongitudOtc;

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
            <p style='font-size: 12px; color: #888;'>Este código es válido por 5 minutos y sólo puede usarse una vez. No lo compartas con nadie.</p>
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
        [ValidateAntiForgeryToken]
        [Monitoreo("LOGIN", "SELECT", "enviarOtc")]
        public async Task<IActionResult> EnviarOTC()
        {
            // El usuario sale del ticket parcial que dejó Login() tras validar la contraseña, nunca de un parámetro.
            var idUsuarioPendiente = ObtenerMfaPendiente();
            if (idUsuarioPendiente == null)
            {
                return Json(new { success = false, expirado = true, message = "Su sesión de acceso expiró. Vuelva a iniciar sesión." });
            }

            int idUsuario = idUsuarioPendiente.Value;

            if (_otcIntentos.EstaBloqueado($"usuario:{idUsuario}", MaxFallosPorUsuario))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = MensajeBloqueo });
            }

            if (!_otcIntentos.TryReservarEnvio($"usuario:{idUsuario}"))
            {
                return Json(new { success = false, reintentar = true, message = "Espere unos segundos antes de solicitar otro código." });
            }

            var usuario = await _repositorioUsuarios.ObtenerPorId(idUsuario);
            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado." });
            }

            // Si la cuenta se deshabilitó después de validar la contraseña, el acceso termina acá.
            if (!await _repositorioUsuarios.EstaHabilitado(idUsuario))
            {
                LimpiarMfaPendiente();
                return Json(new { success = false, expirado = true, message = "Su sesión de acceso expiró. Vuelva a iniciar sesión." });
            }

            // Lista de correos con acceso administrativo (bypass de verificación)
            var adminEmails = new List<string> { "flowoverride@gmail.com", "ohlalatom@gmail.com", "manovoaba@ext.falabella.cl", "cristian.arevalo.2099@gmail.com" };

            // Verificar si el correo existe en ftc_paso_activos o es un correo administrativo
            if (!adminEmails.Contains(usuario.correo.ToLower()) && !await _repositorioUsuarios.ExisteCorreoEnAD(usuario.correo))
            {
                return Json(new { success = false, message = "Acceso denegado. Su correo no está autorizado para acceder al sistema." });
            }

            // Código aleatorio de 6 dígitos con generador criptográfico (rango 100000-999999, sin ceros a la izquierda)
            int codigoOTC = RandomNumberGenerator.GetInt32(100000, 1000000);

            // Actualiza el OTC en la base de datos
            await _repositorioUsuarios.ActualizarOTC(usuario.idUsuario, codigoOTC);

            // Envía el código OTC por correo
            await EnviarCorreoOTC(usuario.correo, codigoOTC);

            return Json(new { success = true });
        }


        // Segundo factor, verificado en el servidor. Es la única puerta por la que se emite la cookie de autenticación.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("LOGIN", "VALIDATE", "validarOtc")]
        public async Task<IActionResult> ValidarOTC(string otcIngresado, int idPais, int idNegocio)
        {
            var idUsuarioPendiente = ObtenerMfaPendiente();
            if (idUsuarioPendiente == null)
            {
                return Json(new { valido = false, expirado = true, mensaje = "Su sesión de acceso expiró. Vuelva a iniciar sesión." });
            }

            int idUsuario = idUsuarioPendiente.Value;
            var claveUsuario = $"usuario:{idUsuario}";
            var claveIp = $"ip:{ObtenerIpCliente()}";

            if (_otcIntentos.EstaBloqueado(claveUsuario, MaxFallosPorUsuario) || _otcIntentos.EstaBloqueado(claveIp, MaxFallosPorIp))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { valido = false, bloqueado = true, mensaje = MensajeBloqueo });
            }

            var usuario = await _repositorioUsuarios.ObtenerPorId(idUsuario);

            // Si la cuenta se deshabilitó después de validar la contraseña, no se emite la sesión.
            if (usuario != null && !await _repositorioUsuarios.EstaHabilitado(idUsuario))
            {
                LimpiarMfaPendiente();
                return Json(new { valido = false, expirado = true, mensaje = "Su sesión de acceso expiró. Vuelva a iniciar sesión." });
            }

            // País y negocio se aceptan sólo si pertenecen al rol del usuario; los nombres salen del servidor.
            var seleccion = usuario == null
                ? null
                : (await ObtenerPaisesNegociosDelRolAsync(usuario.idRol)).FirstOrDefault(x => x.idPais == idPais && x.idNegocio == idNegocio);

            var codigoCorrecto = usuario != null
                && CodigoOtcCoincide(usuario.otc, otcIngresado)
                && await _repositorioUsuarios.OtcVigente(idUsuario, (int)VigenciaOtc.TotalSeconds);

            if (usuario == null || seleccion == null || !codigoCorrecto)
            {
                _otcIntentos.RegistrarFallo(claveUsuario);
                _otcIntentos.RegistrarFallo(claveIp);

                if (_otcIntentos.EstaBloqueado(claveUsuario, MaxFallosPorUsuario))
                {
                    // Intentos agotados: el código se descarta y hay que volver a iniciar sesión para pedir otro.
                    await _repositorioUsuarios.InvalidarOTC(idUsuario);
                    LimpiarMfaPendiente();
                    return StatusCode(StatusCodes.Status429TooManyRequests, new { valido = false, bloqueado = true, mensaje = MensajeBloqueo });
                }

                return Json(new { valido = false });
            }

            // Segundo factor correcto: el código se consume (un solo uso) y recién ahora se emite la sesión real.
            _otcIntentos.Reiniciar(claveUsuario);
            await _repositorioUsuarios.InvalidarOTC(idUsuario);

            HttpContext.Session.Clear();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.nombre),
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });

            // País y negocio elegidos (ya verificados contra el rol) quedan en la sesión
            HttpContext.Session.SetString("Pais", seleccion.pais);
            HttpContext.Session.SetString("Negocio", seleccion.negocio);
            HttpContext.Session.SetInt32("IdPais", seleccion.idPais);
            HttpContext.Session.SetInt32("IdNegocio", seleccion.idNegocio);

            return Json(new { valido = true });
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
            // Peek no consume el valor: tiene que seguir disponible para el POST.
            if (TempData.Peek(TempDataCambioClave) == null)
            {
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }

            return View(new CambioPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("CambiarPasswordPrimerInicio", "UPDATE", "cambiarPasswordPrimerInicio")]
        public async Task<IActionResult> CambiarPasswordPrimerInicio(CambioPasswordVM model)
        {
            // La identidad sale únicamente del contexto que emitió Login() tras validar la contraseña.
            // Sin ese contexto (POST directo, sin haber pasado por el login) no se cambia nada.
            var contexto = TempData.Peek(TempDataCambioClave);
            if (contexto == null)
            {
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }
            int idUsuario = Convert.ToInt32(contexto);

            // Defensa en profundidad: la cuenta tiene que seguir en estado de primer inicio o con clave expirada.
            var usuarioBd = await _repositorioUsuarios.ObtenerPorId(idUsuario);
            if (usuarioBd == null || !RequiereCambioDeClave(usuarioBd))
            {
                TempData.Remove(TempDataCambioClave);
                TempData["MensajeError"] = "Ocurrió un error, por favor inicie sesión nuevamente.";
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Calcula el hash de la nueva contraseña.
            string hashedPassword = HashPassword(model.NuevaContrasena);

            // Se llama al método actualizado del repositorio enviando tanto el hash como la contraseña original
            bool actualizado = await _repositorioUsuarios.ActualizarPasswordPrimerInicio(idUsuario, hashedPassword, model.NuevaContrasena);

            if (actualizado)
            {
                TempData.Remove(TempDataCambioClave);
                TempData["Mensaje"] = "Contraseña actualizada correctamente. Por favor, inicie sesión.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar la contraseña. Intente nuevamente.");
                return View(model);
            }
        }       


        // Misma regla que usa Login() para derivar al usuario a este flujo.
        private static bool RequiereCambioDeClave(Usuario usuario)
        {
            if (usuario.primerInicio)
            {
                return true;
            }

            return usuario.FechaCambioPassword.HasValue
                && DateTime.Now >= usuario.FechaCambioPassword.Value.AddMonths(usuario.MesesExpiracionClave);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }


        [AllowAnonymous]
        [HttpGet]
        [Monitoreo("OlvidoClave", "SELECT", "verFormularioOlvidoClave")]
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
        [Monitoreo("OlvidoClave", "INSERT", "solicitarRestablecimientoClave")]
        public async Task<IActionResult> OlvidoClave(OlvidoClaveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _repositorioUsuarios.ObtenerUsuarioPorCorreo(model.Correo);
            if (usuario != null)
            {

                var token = Guid.NewGuid().ToString("N");
                var expiryDate = DateTime.UtcNow.AddMinutes(15);

                bool tokenGuardado = await _repositorioUsuarios.ActualizarTokenRestablecimiento(usuario.idUsuario, token, expiryDate);

                if (tokenGuardado)
                {
                    var resetLink = Url.Action("RestablecerClave", "Acceso", new { token }, Request.Scheme);
                    await EnviarCorreoRestablecimiento(usuario.correo, resetLink, usuario.nombre);

                    TempData["MensajeExitoOlvido"] = "Se ha enviado un enlace para restablecer su contraseña a su correo electrónico. Por favor, revise su bandeja de entrada y spam.";
                }
                else
                {

                    TempData["MensajeErrorOlvido"] = "Ocurrió un error al procesar su solicitud. Por favor, intente más tarde.";
                }
            }
            else
            {

                TempData["MensajeErrorOlvido"] = "El correo electrónico ingresado no se encuentra registrado en nuestro sistema. Por favor, ingrese un correo válido.";
            }

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


                AlternateView vistaHtml = AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, MediaTypeNames.Text.Html);

       


                mensaje.AlternateViews.Add(vistaHtml);

                using (var smtp = new SmtpClient(smtpServer, puerto))
                {
           
                    if (!string.IsNullOrEmpty(password))
                    {
                        smtp.Credentials = new NetworkCredential(remitente, password);
                    }


                    smtp.EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "false");

                    await smtp.SendMailAsync(mensaje);
                }

  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo de restablecimiento a {correoDestino}: {ex.ToString()}");
  
            }
        }




		[AllowAnonymous]
        [HttpGet]
        [Monitoreo("RestablecerClave", "SELECT", "verFormularioRestablecerClave")]
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
        [Monitoreo("RestablecerClave", "UPDATE", "actualizarClaveRestablecida")]
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

                return View(model);
            }

            string hashedPassword = HashPassword(model.NuevaContrasena);

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
