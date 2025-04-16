using Microsoft.AspNetCore.Authorization;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;


namespace ABM.Controllers
{
    public class MantenedoresController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IConfiguration _configuration;

        public MantenedoresController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IConfiguration configuration)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _configuration = configuration;
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                return View(model);
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
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

        [HttpGet]
        public async Task<IActionResult> ListarRoles()
        {
            var rolesConPNS = await _repositorioRoles.ObtenerRolesConPNS();
            return View(rolesConPNS);
        }

        // En tu GET CrearRolWizard:
        [HttpGet]
        public async Task<IActionResult> CrearRolWizard()
        {
            var model = new RolWizardViewModel();

            // 1) obtén la lista de modelos
            var pnsModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();

            // 2) guárdala sin transformar para el paso 4
            ViewBag.PNSModelList = pnsModelList;

            // 3) si la necesitas como SelectList (p.ej. en un <select>), sigue creando otra:
            ViewBag.PaisNegocioList = pnsModelList
                .Select(p => new SelectListItem
                {
                    Value = p.IdPaisNegocioSistema.ToString(),
                    Text = $"{p.Pais} - {p.Negocio} - {p.Sistema}"
                })
                .ToList();

            // resto de tu código...
            ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
            return View("CrearRolUnicaVista", model);
        }


        // POST: Recibe toda la data del wizard y crea el rol
        [HttpPost]
        public async Task<IActionResult> CrearRolWizard(RolWizardViewModel model)
        {
            // validaciones estándar
            if (!ModelState.IsValid)
            {
                // recarga las dos listas
                ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
                var pnsList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
                ViewBag.PaisNegocioList = pnsList
                    .Select(p => new SelectListItem
                    {
                        Value = p.IdPaisNegocioSistema.ToString(),
                        Text = $"{p.Pais} - {p.Negocio} - {p.Sistema}"
                    })
                    .ToList();
                return View("CrearRolUnicaVista", model);
            }

            // 1) Crear el Rol
            var nuevoRol = new RolModel
            {
                Nombre = model.NombreRol,
                IdVistaInicio = model.MenuInicioSeleccionadoId.Value,
                // estos valores ahora no se usan para detalle_rol
                IdPais = 0,
                IdNegocio = 0
            };
            int idRol = await _repositorioRoles.CrearRol(nuevoRol);

            // 2) Insertar detalle_rol por cada PNS seleccionado
            foreach (var pnsId in model.ListaPNSSeleccionados)
            {
                await _repositorioRoles.CrearDetalleRol(new DetalleRolModel
                {
                    IdRol = idRol,
                    IdPaisNegocioSistema = pnsId
                });
            }

            // 3) Permisos de menú
            if (model.ListaMenusSeleccionados?.Any() == true)
                await _repositorioRoles.InsertarPermisosMenu(idRol, model.ListaMenusSeleccionados);

            TempData["SuccessMessage"] = "Rol creado exitosamente";
            return RedirectToAction("Index", "Home");
        }


    }
}