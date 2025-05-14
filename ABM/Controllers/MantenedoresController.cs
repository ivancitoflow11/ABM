using Microsoft.AspNetCore.Authorization;
using ABM.Models;
using ABM.Servicios;
using ABM.Filters;
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
    [Authorize]
    public class MantenedoresController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IRepositorioListaBlanca _repositorioListaBlanca;
        private readonly IConfiguration _configuration;

        public MantenedoresController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IRepositorioListaBlanca repositorioListaBlanca, IConfiguration configuration)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _repositorioListaBlanca = repositorioListaBlanca;
            _configuration = configuration;
        }

        [HttpGet]
        [Monitoreo("Registrarse", "SELECT", "verFormRegistrarse")]
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
        [Monitoreo("Registrarse", "INSERT", "registrarUsuario")]
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
                    telefono = string.IsNullOrWhiteSpace(model.Telefono) ? 0 : int.Parse(model.Telefono),
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
        [Monitoreo("ListaUsuarios", "SELECT", "verListaUsuarios")]
        public async Task<IActionResult> ListaUsuarios()
        {
            var usuarios = await _repositorioUsuarios.ObtenerTodosLosUsuariosYRoles();
            return View(usuarios);
        }

        [HttpGet]
        [Monitoreo("EditarUsuario", "SELECT", "verEditarUsuario")]
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
        [Monitoreo("EditarUsuario", "UPDATE", "editarUsuario")]
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
        [Monitoreo("ListarRoles", "SELECT", "verListarRoles")]
        public async Task<IActionResult> ListarRoles()
        {
            var rolesConPNS = await _repositorioRoles.ObtenerRolesConPNS();
            return View(rolesConPNS);
        }

        // --- Crear Rol ---
        [HttpGet]
        [Monitoreo("CrearRolWizard", "SELECT", "verCrearRolWizard")]
        public async Task<IActionResult> CrearRolWizard()
        {
            var model = new RolWizardViewModel();
            // Asegurar que las listas para los pasos se cargan
            ViewBag.PNSModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
            ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
            // No es necesario ViewBag.IsEditMode aquí
            return View("CrearRolUnicaVista", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("CrearRolWizard", "INSERT", "crearRolWizard")]
        public async Task<IActionResult> CrearRolWizard(RolWizardViewModel model)
        {
            bool existeRol = await _repositorioRoles.ExisteRolConNombre(model.NombreRol);
            if (existeRol)
            {
                ModelState.AddModelError("NombreRol", "El nombre del rol ya existe");
            }

            if (!model.MenuInicioSeleccionadoId.HasValue || model.MenuInicioSeleccionadoId.Value == 0)
            {
                ModelState.AddModelError("MenuInicioSeleccionadoId", "Debes elegir un Menú de inicio.");
            }
            if (model.ListaPNSSeleccionados == null || !model.ListaPNSSeleccionados.Any())
            {
                ModelState.AddModelError("ListaPNSSeleccionados", "Debe seleccionar al menos un País/Negocio/Sistema.");
            }
            if (model.ListaMenusSeleccionados == null || !model.ListaMenusSeleccionados.Any())
            {
                // Aunque el wizard lo valida en cliente, una validación de servidor es buena.
                ModelState.AddModelError("ListaMenusSeleccionados", "Debe seleccionar al menos un menú.");
            }


            if (!ModelState.IsValid)
            {
                ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
                ViewBag.PNSModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
                return View("CrearRolUnicaVista", model);
            }

            var nuevoRol = new RolModel
            {
                Nombre = model.NombreRol,
                IdVistaInicio = model.MenuInicioSeleccionadoId.Value,
                IdPais = 0,
                IdNegocio = 0
            };
            int idRol = await _repositorioRoles.CrearRol(nuevoRol);

            if (model.ListaPNSSeleccionados != null) // Verificar nulidad
            {
                foreach (var pnsId in model.ListaPNSSeleccionados)
                {
                    await _repositorioRoles.CrearDetalleRol(new DetalleRolModel
                    {
                        IdRol = idRol,
                        IdPaisNegocioSistema = pnsId
                    });
                }
            }

            if (model.ListaMenusSeleccionados?.Any() == true)
            {
                await _repositorioRoles.InsertarPermisosMenu(idRol, model.ListaMenusSeleccionados);
            }

            TempData["SuccessMessage"] = "Rol creado exitosamente";
            return RedirectToAction("ListarRoles");
        }

        // --- Editar Rol ---
        [HttpGet]
        [Monitoreo("EditarRolWizard", "SELECT", "verEditarRolWizard")]
        public async Task<IActionResult> EditarRolWizard(int id) // id del rol
        {
            var rolDb = await _repositorioRoles.ObtenerRolPorId(id);
            if (rolDb == null)
            {
                TempData["ErrorMessage"] = "Rol no encontrado.";
                return RedirectToAction("ListarRoles");
            }

            var model = new RolWizardViewModel
            {
                IdRol = rolDb.IdRol,
                NombreRol = rolDb.Nombre,
                MenuInicioSeleccionadoId = rolDb.IdVistaInicio,
                ListaMenusSeleccionados = await _repositorioRoles.ObtenerMenusSeleccionadosPorRol(id),
                ListaPNSSeleccionados = await _repositorioRoles.ObtenerPNSSeleccionadosPorRol(id)
            };

            ViewBag.PNSModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
            ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
            // No es necesario ViewBag.IsEditMode aquí, ya que es una vista dedicada
            return View("EditarRolWizard", model); // Nueva vista para editar
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EditarRolWizard", "UPDATE", "editarRolWizard")]
        public async Task<IActionResult> EditarRolWizard(RolWizardViewModel model) // El model ya incluye IdRol
        {
            if (!model.IdRol.HasValue) // Seguridad básica
            {
                TempData["ErrorMessage"] = "Error al identificar el rol a editar.";
                return RedirectToAction("ListarRoles");
            }

            bool existeRolConMismoNombre = await _repositorioRoles.ExisteRolConNombre(model.NombreRol, model.IdRol.Value);
            if (existeRolConMismoNombre)
            {
                ModelState.AddModelError("NombreRol", "El nombre del rol ya existe para otro rol.");
            }

            if (!model.MenuInicioSeleccionadoId.HasValue || model.MenuInicioSeleccionadoId.Value == 0)
            {
                ModelState.AddModelError("MenuInicioSeleccionadoId", "Debes elegir un Menú de inicio.");
            }
            if (model.ListaPNSSeleccionados == null || !model.ListaPNSSeleccionados.Any())
            {
                ModelState.AddModelError("ListaPNSSeleccionados", "Debe seleccionar al menos un País/Negocio/Sistema.");
            }
            if (model.ListaMenusSeleccionados == null || !model.ListaMenusSeleccionados.Any())
            {
                ModelState.AddModelError("ListaMenusSeleccionados", "Debe seleccionar al menos un menú.");
            }


            if (!ModelState.IsValid)
            {
                ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
                ViewBag.PNSModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
                return View("EditarRolWizard", model); // Apuntar a la vista de edición en caso de error
            }

            try
            {
                var rolActualizar = new RolModel
                {
                    IdRol = model.IdRol.Value,
                    Nombre = model.NombreRol,
                    IdVistaInicio = model.MenuInicioSeleccionadoId.Value,
                    IdPais = 0,
                    IdNegocio = 0
                };
                await _repositorioRoles.ActualizarRol(rolActualizar);
                await _repositorioRoles.ActualizarDetallesRol(model.IdRol.Value, model.ListaPNSSeleccionados ?? new List<int>());
                await _repositorioRoles.ActualizarPermisosMenu(model.IdRol.Value, model.ListaMenusSeleccionados ?? new List<int>());

                TempData["SuccessMessage"] = "Rol actualizado exitosamente";
                return RedirectToAction("ListarRoles");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar el rol: {ex.Message}";
                ViewBag.MenusDisponibles = await _repositorioRoles.ObtenerMenus();
                ViewBag.PNSModelList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
                return View("EditarRolWizard", model); // Apuntar a la vista de edición en caso de error
            }
        }

        [HttpGet]
        [Monitoreo("VerificarNombreRol", "SELECT", "verificarNombreRol")]
        public async Task<IActionResult> VerificarNombreRol(string nombre, int? idRol)
        {
            if (string.IsNullOrEmpty(nombre))
                return Json(new { existe = false });

            bool existe = await _repositorioRoles.ExisteRolConNombre(nombre, idRol);
            return Json(new { existe });
        }


        // --- MANTENEDOR LISTA BLANCA ---

        [HttpGet]
        [Monitoreo("ListarEntradasListaBlanca", "SELECT", "verListaBlanca")]
        public async Task<IActionResult> ListarEntradasListaBlanca()
        {
            var modelo = await _repositorioListaBlanca.ObtenerTodos();
            return View(modelo);
        }

        [HttpGet]
        [Monitoreo("CrearEntradaListaBlanca", "SELECT", "verFormCrearListaBlanca")]
        public IActionResult CrearEntradaListaBlanca()
        {
            return View(new ListaBlancaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("CrearEntradaListaBlanca", "INSERT", "crearEntradaListaBlanca")]
        public async Task<IActionResult> CrearEntradaListaBlanca(ListaBlancaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _repositorioListaBlanca.Existe(model.RutNumDocumento))
            {
                ModelState.AddModelError("RutNumDocumento", "El RUT/Núm. Documento ya existe en la lista blanca.");
                return View(model);
            }

            // Generar APELLIDO_NOMBRE si no viene del formulario o es calculado
            model.ApellidoNombre = $"{model.Apellidos} {model.Nombres}".Trim();

            var listaBlanca = new ListaBlanca
            {
                RutNumDocumento = model.RutNumDocumento,
                RUT = model.RUT,
                DV = model.DV,
                NumeroEmpleado = model.NumeroEmpleado,
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                ApellidoNombre = model.ApellidoNombre, // Asignar el campo generado
                Negocio = model.Negocio,
                Pais = model.Pais,
                Cargo = model.Cargo,
                Departamento = model.Departamento,
                TipoEmpleado = model.TipoEmpleado,
                ActivoFalanet = model.ActivoFalanet,
                FiniquitadosFalanet = model.FiniquitadosFalanet,
                ActivoAd = model.ActivoAd
            };

            bool creado = await _repositorioListaBlanca.Crear(listaBlanca);

            if (creado)
            {
                TempData["SuccessMessage"] = "Entrada de lista blanca creada correctamente.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }
            else
            {
                TempData["ErrorMessage"] = "Error al crear la entrada en la lista blanca.";
                return View(model);
            }
        }

        [HttpGet]
        [Monitoreo("EditarEntradaListaBlanca", "SELECT", "verFormEditarListaBlanca")]
        public async Task<IActionResult> EditarEntradaListaBlanca(string id) // id es RutNumDocumento
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }
            var entrada = await _repositorioListaBlanca.ObtenerPorRutNumDocumento(id);
            if (entrada == null)
            {
                TempData["ErrorMessage"] = "Entrada no encontrada en la lista blanca.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }

            var viewModel = new ListaBlancaViewModel
            {
                RutNumDocumento = entrada.RutNumDocumento,
                RUT = entrada.RUT,
                DV = entrada.DV,
                NumeroEmpleado = entrada.NumeroEmpleado,
                Nombres = entrada.Nombres,
                Apellidos = entrada.Apellidos,
                ApellidoNombre = entrada.ApellidoNombre,
                Negocio = entrada.Negocio,
                Pais = entrada.Pais,
                Cargo = entrada.Cargo,
                Departamento = entrada.Departamento,
                TipoEmpleado = entrada.TipoEmpleado,
                ActivoFalanet = entrada.ActivoFalanet,
                FiniquitadosFalanet = entrada.FiniquitadosFalanet,
                ActivoAd = entrada.ActivoAd
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EditarEntradaListaBlanca", "UPDATE", "editarEntradaListaBlanca")]
        public async Task<IActionResult> EditarEntradaListaBlanca(string id, ListaBlancaViewModel model)
        {
            if (id != model.RutNumDocumento)
            {
                TempData["ErrorMessage"] = "Error de concordancia de ID.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generar APELLIDO_NOMBRE si es necesario
            model.ApellidoNombre = $"{model.Apellidos} {model.Nombres}".Trim();

            var entrada = new ListaBlanca
            {
                RutNumDocumento = model.RutNumDocumento,
                RUT = model.RUT,
                DV = model.DV,
                NumeroEmpleado = model.NumeroEmpleado,
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                ApellidoNombre = model.ApellidoNombre,
                Negocio = model.Negocio,
                Pais = model.Pais,
                Cargo = model.Cargo,
                Departamento = model.Departamento,
                TipoEmpleado = model.TipoEmpleado,
                ActivoFalanet = model.ActivoFalanet,
                FiniquitadosFalanet = model.FiniquitadosFalanet,
                ActivoAd = model.ActivoAd
            };

            bool actualizado = await _repositorioListaBlanca.Actualizar(entrada);

            if (actualizado)
            {
                TempData["SuccessMessage"] = "Entrada de lista blanca actualizada correctamente.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }
            else
            {
                TempData["ErrorMessage"] = "Error al actualizar la entrada en la lista blanca.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EliminarEntradaListaBlanca", "DELETE", "eliminarEntradaListaBlanca")]
        public async Task<IActionResult> EliminarEntradaListaBlancaConfirmado(string id) // id es RutNumDocumento
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID no proporcionado para eliminar.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }

            var existe = await _repositorioListaBlanca.Existe(id);
            if (!existe)
            {
                TempData["ErrorMessage"] = "Entrada no encontrada para eliminar.";
                return RedirectToAction("ListarEntradasListaBlanca");
            }

            bool eliminado = await _repositorioListaBlanca.Eliminar(id);

            if (eliminado)
            {
                TempData["SuccessMessage"] = "Entrada eliminada correctamente de la lista blanca.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al eliminar la entrada de la lista blanca.";
            }
            return RedirectToAction("ListarEntradasListaBlanca");
        }
    }

}
