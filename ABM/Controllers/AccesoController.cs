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

                TempData["SuccessMessage"] = "Usuario registrado correctamente.";
                return RedirectToAction("ListaUsuarios");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al registrar el usuario. Intente nuevamente.";
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }
        }


        [AllowAnonymous]
        [HttpGet]
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

        [HttpGet]
        public async Task<IActionResult> ListaUsuarios()
        {
            var usuarios = await _repositorioUsuarios.ObtenerTodosLosUsuariosYRoles();
            return View(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _repositorioUsuarios.ObtenerPorId(id);

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("ListaUsuarios");
            }

            var roles = await _repositorioUsuarios.ObtenerRoles();

            var model = new EditarUsuarioVM
            {
                IdUsuario = usuario.idUsuario,
                Nombre = usuario.nombre,
                Apellidos = usuario.apellidos,
                Rut = usuario.rut,
                Telefono = usuario.telefono,
                Correo = usuario.correo,
                Usuario = usuario.usuario,
                RolId = usuario.idRol,
                RolesDisponibles = roles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(EditarUsuarioVM model)
        {
            if (!ModelState.IsValid)
            {
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            var usuarioExistente = await _repositorioUsuarios.ObtenerPorId(model.IdUsuario);
            if (usuarioExistente == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("ListaUsuarios");
            }

            // Verifica si otro usuario ya usa el mismo correo
            var correoUsado = await _repositorioUsuarios.ExisteCorreo(model.Correo);
            if (correoUsado && !string.Equals(usuarioExistente.correo, model.Correo, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Correo", "El correo ya se encuentra registrado.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            // Verifica si otro usuario ya usa el mismo nombre de usuario
            var usuarioUsado = await _repositorioUsuarios.ExisteUsuario(model.Usuario);
            if (usuarioUsado && !string.Equals(usuarioExistente.usuario, model.Usuario, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Usuario", "El nombre de usuario ya está en uso.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }

            // Actualiza
            usuarioExistente.nombre = model.Nombre;
            usuarioExistente.apellidos = model.Apellidos;
            usuarioExistente.rut = model.Rut;
            usuarioExistente.telefono = model.Telefono;
            usuarioExistente.correo = model.Correo;
            usuarioExistente.usuario = model.Usuario;
            usuarioExistente.idRol = model.RolId;

            bool actualizado = await _repositorioUsuarios.ActualizarUsuario(usuarioExistente);

            if (actualizado)
            {
                TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                return RedirectToAction("ListaUsuarios");
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar el usuario.");
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
