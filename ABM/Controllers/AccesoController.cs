using Microsoft.AspNetCore.Mvc;
using ABM.Data;
using ABM.Models;
using Microsoft.EntityFrameworkCore;
using ABM.ViewModels;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using ABM.Helpers;
using ABM.Servicios;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;


namespace ABM.Controllers
{
    public class AccesoController : Controller
    {
        private readonly AppDBContext _appDBContext;
        private readonly IConfiguration _configuration;
		private readonly ABM.Servicios.IEmailSender _emailSender;
		private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IRepositorioCargas repositorioCargas;
        private readonly IRepositorioMenus repositorioMenus;

		public AccesoController(AppDBContext appDBContext, IConfiguration configuration, ABM.Servicios.IEmailSender emailSender, IRepositorioUsuarios RepositorioUsuarios, IRepositorioCargas RepositorioCargas, IRepositorioMenus RepositorioMenus)
		{
			_appDBContext = appDBContext;
			_configuration = configuration;
			_emailSender = emailSender;
			repositorioUsuarios = RepositorioUsuarios;
			repositorioCargas = RepositorioCargas;
			repositorioMenus = RepositorioMenus;
		}

		[Authorize]
        [HttpGet]
        public async Task<IActionResult> Registrarse(int? codGerencia)
        {
            UsuarioVM model = new UsuarioVM();
            model.Roles = await repositorioUsuarios.ObtenerRoles();
            model.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();

            // Cargar subgerencias solo si se proporciona una gerencia
            model.ListaSubgerencias = codGerencia.HasValue
                ? await repositorioUsuarios.ObtenerSubGerencias(codGerencia)
                : new List<Subgerencia>();

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Registrarse(UsuarioVM modelo)
        {
            try
            {
                // Validación de contraseñas
                if (modelo.password_c != modelo.ConfirmarClave)
                {
                    ViewData["Error"] = "Las contraseñas no coinciden.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Validación del rol
                if (modelo.idRol <= 0)
                {
                    ViewData["Error"] = "Debes seleccionar un rol.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Verificar si el correo ya está registrado
                var existingUser = await _appDBContext.Usuario
                    .Where(u => u.correo == modelo.correo)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    ViewData["Error"] = "El correo electrónico ya está registrado.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Validación de contraseña
                string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.])[A-Za-z\d@$!%*?&.]{8,}$";
                if (!Regex.IsMatch(modelo.password_c, passwordRegex))
                {
                    ViewData["Error"] = "La contraseña no cumple los requisitos de seguridad.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Validación de la firma (Formato Base64 y Tipo MIME)
                if (string.IsNullOrEmpty(modelo.firma))
                {
                    ViewData["Error"] = "Por favor, añade una firma.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Extraer tipo MIME de la firma Base64
                string[] firmaParts = modelo.firma.Split(',');
                if (firmaParts.Length != 2 || !firmaParts[0].StartsWith("data:image/"))
                {
                    ViewData["Error"] = "La firma debe ser un archivo de imagen válido (JPG, PNG, JPEG, etc.).";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Validar extensiones permitidas
                string mimeType = firmaParts[0].Split(':')[1].Split(';')[0];
                var formatosPermitidos = new List<string> { "image/png", "image/jpeg", "image/jpg" };
                if (!formatosPermitidos.Contains(mimeType))
                {
                    ViewData["Error"] = "Formato de imagen no permitido. Usa PNG, JPG o JPEG.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Creación del usuario
                Usuario usuario = new Usuario
                {
                    nombre = modelo.nombre,
                    correo = modelo.correo,
                    password_c = modelo.password_c,
                    password = HashPassword(modelo.password_c),
                    idRol = modelo.idRol,
                    estado = modelo.estado,
                    ID_gerencia = modelo.ID_gerencia,
                    ID_Subgerencia = modelo.ID_Subgerencia,
                    firma = modelo.firma,
                    ResponsableFirma = modelo.ResponsableFirma
                };

                await _appDBContext.Usuario.AddAsync(usuario);
                await _appDBContext.SaveChangesAsync();

                if (usuario.IdUsuario != 0)
                {
                    ViewData["Success"] = "Usuario registrado exitosamente.";
                }

                modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                return View(modelo);
            }
            catch (Exception ex)
            {
                // Registro básico en consola
                Console.WriteLine($"Error al registrar usuario: {ex.Message}");
                Console.WriteLine($"Detalles del error: {ex.StackTrace}");

                ViewData["Error"] = "Ocurrió un error al registrar: " + ex.Message;
                modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                return View(modelo);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerSubGerenciasPorGerencia(int? codGerencia)
        {
            // Imprimir el valor recibido para depuración
            Console.WriteLine($"Valor de codGerencia recibido: {codGerencia}");
            Console.WriteLine($"Tipo de codGerencia: {codGerencia?.GetType()}");

            // Si no se selecciona una gerencia o el valor es inválido, devolver una lista vacía
            if (!codGerencia.HasValue || codGerencia <= 0)
            {
                return Json(new List<Subgerencia>());
            }

            // Obtener las subgerencias relacionadas con la gerencia seleccionada
            var subgerencias = await repositorioUsuarios.ObtenerSubGerencias(codGerencia);

            // Retornar las subgerencias en formato JSON
            return Json(subgerencias);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CrearRol()
        {
            UsuarioVM model = new UsuarioVM();
            model.VistasMenu = await repositorioUsuarios.ObtenerMenu();
            model.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
            model.Sistemas = await repositorioCargas.ListaDeSistemas();
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> CrearRolYAsignarPermisos(UsuarioVM modelo)
        {
            // Validar que el nombre del rol no esté vacío
            if (string.IsNullOrEmpty(modelo.nuevoRol))
            {
                ViewData["Error"] = "El nombre del rol no puede estar vacío.";
                // Volver a cargar los datos necesarios para la vista
                modelo.Sistemas = await repositorioCargas.ListaDeSistemas();
                modelo.VistasMenu = await repositorioUsuarios.ObtenerMenu();
                modelo.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
                return View("CrearRol", modelo); // Regresar a la vista con el modelo actual
            }

            // Validar que se haya seleccionado un sistema
            if (modelo.idRol <= 0)
            {
                ViewData["Error"] = "Debes seleccionar un sistema.";
                // Volver a cargar los datos necesarios para la vista
                modelo.Sistemas = await repositorioCargas.ListaDeSistemas();
                modelo.VistasMenu = await repositorioUsuarios.ObtenerMenu();
                modelo.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
                return View("CrearRol", modelo); // Regresar a la vista con el modelo actual
            }

            // Validar que la vista principal esté seleccionada
            if (modelo.codVistaPrincipal <= 0)
            {
                ViewData["Error"] = "Debes seleccionar una vista principal.";
                // Volver a cargar los datos necesarios para la vista
                modelo.Sistemas = await repositorioCargas.ListaDeSistemas();
                modelo.VistasMenu = await repositorioUsuarios.ObtenerMenu();
                modelo.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
                return View("CrearRol", modelo); // Regresar a la vista con el modelo actual
            }

            // Validar que al menos un checkbox de menú o sub-menú esté seleccionado
            if ((modelo.PermisosMenuSeleccionados == null || !modelo.PermisosMenuSeleccionados.Any()) &&
                (modelo.PermisosSubMenuSeleccionados == null || !modelo.PermisosSubMenuSeleccionados.Any()))
            {
                ViewData["Error"] = "Debes seleccionar al menos un permiso de Menú o Sub-Menú.";
                // Volver a cargar los datos necesarios para la vista
                modelo.Sistemas = await repositorioCargas.ListaDeSistemas();
                modelo.VistasMenu = await repositorioUsuarios.ObtenerMenu();
                modelo.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
                return View("CrearRol", modelo); // Regresar a la vista con el modelo actual
            }

            try
            {
                // Crear un nuevo rol
                Rol nuevoRol = new Rol
                {
                    nombre = modelo.nuevoRol
                };

                await repositorioUsuarios.CrearRol(nuevoRol);

                // Asignar permisos de Menú
                if (modelo.PermisosMenuSeleccionados != null && modelo.PermisosMenuSeleccionados.Any())
                {
                    foreach (var menuId in modelo.PermisosMenuSeleccionados)
                    {
                        await repositorioUsuarios.GuardarPermisosMenu(menuId, nuevoRol.idRol, modelo.codVistaPrincipal);
                    }
                }

                // Asignar permisos de Sub-Menú
                if (modelo.PermisosSubMenuSeleccionados != null && modelo.PermisosSubMenuSeleccionados.Any())
                {
                    foreach (var subMenuId in modelo.PermisosSubMenuSeleccionados)
                    {
                        await repositorioUsuarios.GuardarPermisosSubMenu(subMenuId, nuevoRol.idRol);
                    }
                }

                ViewData["Success"] = "Rol creado y permisos asignados exitosamente.";
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Hubo un error al crear el rol o asignar permisos: " + ex.Message;
            }

            // Volver a cargar los datos necesarios para la vista
            modelo.Sistemas = await repositorioCargas.ListaDeSistemas();
            modelo.VistasMenu = await repositorioUsuarios.ObtenerMenu();
            modelo.VistasSubMenu = await repositorioUsuarios.ObtenerSubMenu();
            return View("CrearRol", modelo); // Regresar a la vista con el modelo actual
        }




        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id, int? codGerencia)
        {
            var usuario = await _appDBContext.Usuario.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var modelo = new UsuarioVM
            {
                IdUsuario = usuario.IdUsuario,
                nombre = usuario.nombre,
                correo = usuario.correo,
                idRol = usuario.idRol,
                estado = usuario.estado,
                ID_gerencia = usuario.ID_gerencia,
                ID_Subgerencia = usuario.ID_Subgerencia,
                ResponsableFirma = usuario.ResponsableFirma
            };

            modelo.Roles = await repositorioUsuarios.ObtenerRoles();
            modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
            modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(codGerencia);

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> EditarUsuario(UsuarioVM modelo)
        {
            // Buscar el usuario existente
            var usuarioExistente = await _appDBContext.Usuario.FindAsync(modelo.IdUsuario);

            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // Verificar si se está cambiando la contraseña
            if (!string.IsNullOrEmpty(modelo.password_c))
            {
                // Verificar si las contraseñas coinciden
                if (modelo.password_c != modelo.ConfirmarClave)
                {
                    TempData["Error"] = "Las contraseñas no coinciden.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Verificar si la contraseña es válida
                string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.])[A-Za-z\d@$!%*?&.]{8,}$";
                if (!Regex.IsMatch(modelo.password_c, passwordRegex))
                {
                    TempData["Error"] = "La contraseña debe tener al menos 8 caracteres, una letra mayúscula, una letra minúscula, un número y un caracter especial.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Actualizar contraseñas
                usuarioExistente.password_c = modelo.password_c;
                usuarioExistente.password = HashPassword(modelo.password_c);
            }
            if (!string.IsNullOrEmpty(modelo.firma))
            {
                // Extraer tipo MIME de la firma Base64
                string[] firmaParts = modelo.firma.Split(',');
                if (firmaParts.Length != 2 || !firmaParts[0].StartsWith("data:image/"))
                {
                    TempData["Error"] = "La firma debe ser un archivo de imagen válido (JPG, PNG, JPEG, etc.).";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Validar extensiones permitidas
                string mimeType = firmaParts[0].Split(':')[1].Split(';')[0];
                var formatosPermitidos = new List<string> { "image/png", "image/jpeg", "image/jpg" };
                if (!formatosPermitidos.Contains(mimeType))
                {
                    TempData["Error"] = "Formato de imagen no permitido. Usa PNG, JPG o JPEG.";
                    modelo.Roles = await repositorioUsuarios.ObtenerRoles();
                    modelo.ListaGerencias = await repositorioUsuarios.ObtenerGerencias();
                    modelo.ListaSubgerencias = await repositorioUsuarios.ObtenerSubGerencias(modelo.ID_gerencia);
                    return View(modelo);
                }

                // Actualizar firma
                usuarioExistente.firma = modelo.firma;
            }

            // Actualizar otros campos
            usuarioExistente.nombre = modelo.nombre;
            usuarioExistente.correo = modelo.correo;
            usuarioExistente.idRol = modelo.idRol;
            usuarioExistente.estado = modelo.estado;
            usuarioExistente.ID_gerencia = modelo.ID_gerencia;
            usuarioExistente.ID_Subgerencia = modelo.ID_Subgerencia;
            usuarioExistente.ResponsableFirma = modelo.ResponsableFirma;

            // Guardar cambios
            _appDBContext.Update(usuarioExistente);
            await _appDBContext.SaveChangesAsync();

            // Mensaje de éxito y redirección
            TempData["Success"] = "Usuario actualizado exitosamente.";
            return RedirectToAction("ListaUsuarios", "Home");
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginVM modelo)
        {
            // Buscar el usuario incluyendo Gerencia y Subgerencia
            Usuario usuario_encontrado = await _appDBContext.Usuario
                .Include(u => u.Gerencia)
                .Include(u => u.Subgerencia) // Añadido para incluir Subgerencia
                .Where(u =>
                    u.correo == modelo.correo &&
                    u.password_c == modelo.password_c &&
                    u.estado == "1")
                .FirstOrDefaultAsync();

            if (usuario_encontrado == null)
            {
                ViewData["Mensaje"] = "No se encontraron coincidencias o el usuario está inactivo.";
                return View();
            }

            // Verificar si la contraseña es "Imperial.2024"
            if (usuario_encontrado.password_c == "Imperial.2024")
            {
                List<Claim> claims = new List<Claim>
        {
            new Claim("RequiresPasswordChange", "true"),
            new Claim(ClaimTypes.Name, usuario_encontrado.correo),
            new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.IdUsuario.ToString()),
			new Claim("ResponsableFirma", usuario_encontrado.ResponsableFirma.HasValue && usuario_encontrado.ResponsableFirma.Value ? "true" : "false")
		};
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                AuthenticationProperties properties = new AuthenticationProperties()
                {
                    AllowRefresh = true,
                };
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    properties
                );

                return RedirectToAction("CambiarPasswordPrimerInicio");
            }

            // Verificar si la contraseña ha expirado (más de 2 meses desde último cambio)
            if (usuario_encontrado.FechaCambioPassword == null ||
                usuario_encontrado.FechaCambioPassword.Value.AddMonths(2) < DateTime.Now)
            {
                List<Claim> claims = new List<Claim>
        {
            new Claim("RequiresPasswordChange", "true"),
            new Claim(ClaimTypes.Name, usuario_encontrado.correo),
            new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.IdUsuario.ToString())
        };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                AuthenticationProperties properties = new AuthenticationProperties() { AllowRefresh = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    properties
                );

                return RedirectToAction("CambiarPasswordPrimerInicio");
            }

            // Crear los claims de autenticación normal
            List<Claim> userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, usuario_encontrado.correo),
        new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.IdUsuario.ToString()),
		new Claim("ResponsableFirma", usuario_encontrado.ResponsableFirma.HasValue && usuario_encontrado.ResponsableFirma.Value ? "true" : "false")
	};

            // Añadir claim de Gerencia si existe
            if (usuario_encontrado.ID_gerencia != null)
            {
                userClaims.Add(new Claim("ID_gerencia", usuario_encontrado.ID_gerencia.ToString()));
            }

            // Añadir claim de Subgerencia si existe
            if (usuario_encontrado.ID_Subgerencia != null)
            {
                userClaims.Add(new Claim("ID_Subgerencia", usuario_encontrado.ID_Subgerencia.ToString()));

                // Si también quieres añadir el nombre de la Subgerencia
                if (usuario_encontrado.Subgerencia != null)
                {
                    userClaims.Add(new Claim("Nom_Subgerencia", usuario_encontrado.Subgerencia.Nom_Subgerencia));
                }
            }

            ClaimsIdentity identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties authProperties = new AuthenticationProperties()
            {
                AllowRefresh = true,
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties
            );

            // Guardar el ID de usuario en la sesión
            HttpContext.Session.SetInt32("IdUsuarioLogueado", usuario_encontrado.IdUsuario);

            TempData["MostrarAlertasInicio"] = true;
            // Obtener la vista principal
            var vistaPrincipal = await repositorioMenus.ObtenerVistaPrincipal(usuario_encontrado.IdUsuario);
            if (vistaPrincipal != null && vistaPrincipal.Any())
            {
                var menuPrincipal = vistaPrincipal.First();
                if (!string.IsNullOrEmpty(menuPrincipal.DESC_Controlador) && !string.IsNullOrEmpty(menuPrincipal.DESC_Vista))
                {
                    return RedirectToAction(menuPrincipal.DESC_Vista, menuPrincipal.DESC_Controlador);
                }
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult CambiarPasswordPrimerInicio()
        {
            var requiresPasswordChange = User.Claims.FirstOrDefault(c => c.Type == "RequiresPasswordChange" && c.Value == "true");

            if (requiresPasswordChange == null)
            {
                // Si no tiene el claim de cambio de contraseña, redirigir al login
                return RedirectToAction("Login");
            }

            // Obtener el correo desde los claims
            var correo = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(correo))
            {
                // Si no hay un claim válido, redirigir al login
                return RedirectToAction("Login");
            }

            ViewBag.Correo = correo;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CambiarPasswordPrimerInicio(CambioPasswordVM modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            if (modelo.NuevaContrasena != modelo.ConfirmarContrasena)
            {
                ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden.");
                return View(modelo);
            }

            if (modelo.NuevaContrasena == "Imperial.2024")
            {
                ModelState.AddModelError("NuevaContrasena", "No puede usar la contraseña predeterminada.");
                return View(modelo);
            }

            var correoAutenticado = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(correoAutenticado))
                return RedirectToAction("Login", "Acceso");

            var usuario = await _appDBContext.Usuario
                .FirstOrDefaultAsync(u => u.correo == correoAutenticado);

            if (usuario == null)
                return NotFound();

            // Actualizar la contraseña y la fecha
            usuario.password_c = modelo.NuevaContrasena;
            usuario.FechaCambioPassword = DateTime.Now;

            await _appDBContext.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Mensaje"] = "Contraseña actualizada exitosamente. Por favor ingrese sus credenciales nuevamente.";
            return RedirectToAction("Login", "Acceso");
        }





        [AllowAnonymous]
        [HttpGet]
        public IActionResult OlvidoClave()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OlvidoClave(string correo)
        {
            // Verificar si el usuario existe en la base de datos
            var usuario = await _appDBContext.Usuario.FirstOrDefaultAsync(u => u.correo == correo);
            if (usuario == null)
            {
                ViewData["Mensaje"] = "No existe una cuenta asociada a este correo.";
                return View();
            }

            // Generar un token único para la recuperación
            var token = Guid.NewGuid().ToString();
            usuario.TokenRecuperacion = token;
            usuario.ExpiracionToken = DateTime.Now.AddHours(1); // El token expira en 1 hora
            await _appDBContext.SaveChangesAsync();

            // Crear un enlace de recuperación de contraseña
            var urlRecuperacion = Url.Action("ResetClave", "Acceso", new { token }, Request.Scheme);

            // Enviar el correo
            await _emailSender.SendEmailAsync(
                correo,
                "Recuperación de Contraseña",
                $"<p>Hola {usuario.nombre},</p>" +
                $"<p>Hemos recibido una solicitud para restablecer tu contraseña. Haz clic en el enlace de abajo para continuar:</p>" +
                $"<a href='{urlRecuperacion}'>Restablecer Contraseña</a>" +
                $"<p>Si no solicitaste este cambio, ignora este mensaje.</p>"
            );

            ViewData["Mensaje"] = "Por favor, revisa tu correo. Se han enviado instrucciones para recuperar tu clave de acceso.";
            return View();
        }



        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetClave(string token)
        {
            // Verificar si el token es válido
            var usuario = _appDBContext.Usuario.FirstOrDefault(u => u.TokenRecuperacion == token && u.ExpiracionToken > DateTime.Now);
            if (usuario == null)
            {
                ViewData["Mensaje"] = "El enlace de recuperación ha expirado o es inválido";
                return RedirectToAction("Login");
            }

            return View(new ResetClaveVM { Token = token });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetClave(ResetClaveVM modelo)
        {
            // Validar el modelo y mostrar errores en caso de fallos
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Validar el token
            var usuario = await _appDBContext.Usuario.FirstOrDefaultAsync(u =>
                u.TokenRecuperacion == modelo.Token && u.ExpiracionToken > DateTime.Now);

            if (usuario == null)
            {
                TempData["Mensaje"] = "El enlace de recuperación ha expirado o es inválido.";
                return RedirectToAction("Login");
            }

            // Actualizar la contraseña
            usuario.password_c = modelo.NuevaClave;
            usuario.password = HashPassword(modelo.NuevaClave);
            usuario.TokenRecuperacion = null;
            usuario.ExpiracionToken = null;
            await _appDBContext.SaveChangesAsync();

            // Establecer mensaje de éxito
            TempData["Exito"] = "Tu clave ha sido restablecida correctamente.";

            // Renderizar directamente la vista actual para mostrar el SweetAlert
            return View(modelo);
        }





        [HttpPost]
        public IActionResult LimpiarTempData()
        {
            TempData.Remove("MostrarAlertasInicio");
            return Json(new { success = true });
        }
        [AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> ProbarEnvioCorreo()
		{
			try
			{
				await _emailSender.SendEmailAsync(
					"flowoverride@gmail.com", // Cambia esto por el correo al que deseas enviar
					"Prueba de Envío",
					"<p>Este es un correo de prueba desde el sistema Imperial.</p>"
				);

				return Content("¡Correo enviado exitosamente!");
			}
			catch (Exception ex)
			{
				return Content($"Error al enviar correo: {ex.Message}");
			}
		}


	}
}
