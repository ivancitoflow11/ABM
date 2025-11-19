using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Security.Claims;
using System.Threading.Tasks;
using ABM.Filters;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ABM.Controllers
{
    [Authorize]
    public class MantenedoresController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IRepositorioRoles _repositorioRoles;
        private readonly IRepositorioListaBlanca _repositorioListaBlanca;
        private readonly IRepositorioConfiguracion _repositorioConfiguracion;
        private readonly IRepositorioGlosaNegocios _repositorioGlosaNegocios;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private IEnumerable<PaisNegocioSistemaModel> _cachedPnsList;
        public MantenedoresController(IRepositorioUsuarios repositorioUsuarios, IRepositorioRoles repositorioRoles, IRepositorioListaBlanca repositorioListaBlanca, IRepositorioConfiguracion repositorioConfiguracion, IRepositorioGlosaNegocios repositorioGlosaNegocios, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _repositorioRoles = repositorioRoles;
            _repositorioListaBlanca = repositorioListaBlanca;
            _repositorioConfiguracion = repositorioConfiguracion;
            _repositorioGlosaNegocios = repositorioGlosaNegocios;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Monitoreo("Registrarse", "SELECT", "verFormRegistrarse")]
        public async Task<IActionResult> Registrarse()
        {
            // Carga tanto Roles como Gerencias para los dropdowns
            var model = new RegistroUsuarioVM
            {
                RolesDisponibles = await _repositorioUsuarios.ObtenerRoles(),
                GerenciasDisponibles = (await _repositorioConfiguracion.ObtenerGerencias())
                                        .Select(g => new SelectListItem
                                        {
                                            Value = g.IdGerencia.ToString(),
                                            Text = g.Nom_Gerencia
                                        }).ToList()
            };

            return View(model);
        }

        // En tu archivo Controllers/MantenedoresController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("Registrarse", "INSERT", "registrarUsuario")]
        public async Task<IActionResult> Registrarse(RegistroUsuarioVM model)
        {
            // 1. VERIFICACIÓN DEL MODELO
            // Si el modelo no es válido (ej. un campo requerido está vacío), se recargan
            // los datos para los dropdowns y se vuelve a mostrar el formulario con los errores.
            if (!ModelState.IsValid)
            {
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                model.GerenciasDisponibles = (await _repositorioConfiguracion.ObtenerGerencias())
                                                 .Select(g => new SelectListItem { Value = g.IdGerencia.ToString(), Text = g.Nom_Gerencia }).ToList();
                return View(model);
            }

            // 2. VALIDACIONES ADICIONALES (EJ: CONTRASEÑAS)
            if (model.Password != model.Repeat_Password)
            {
                ModelState.AddModelError("Repeat_Password", "Las contraseñas no coinciden.");
                // También hay que recargar los dropdowns antes de volver a la vista
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                model.GerenciasDisponibles = (await _repositorioConfiguracion.ObtenerGerencias())
                                                 .Select(g => new SelectListItem { Value = g.IdGerencia.ToString(), Text = g.Nom_Gerencia }).ToList();
                return View(model);
            }

            // (Aquí puedes agregar otras validaciones como verificar si el correo o usuario ya existen)


            try
            {
                // 3. PROCESAMIENTO DEL ARCHIVO DE FIRMA
                string rutaFirma = null; // Se inicializa como nula

                // Se comprueba si el usuario subió un archivo para el campo 'Firma'
                if (model.Firma != null && model.Firma.Length > 0)
                {
                    // Se define la carpeta de destino dentro de wwwroot
                    string carpetaDestino = Path.Combine(_webHostEnvironment.WebRootPath, "firmas");

                    // Si la carpeta no existe, se crea
                    if (!Directory.Exists(carpetaDestino))
                    {
                        Directory.CreateDirectory(carpetaDestino);
                    }

                    // Se crea un nombre de archivo único para prevenir conflictos y sobreescrituras
                    string nombreArchivoUnico = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Firma.FileName);
                    string rutaCompletaArchivo = Path.Combine(carpetaDestino, nombreArchivoUnico);

                    // Se guarda el archivo físicamente en el servidor
                    using (var fileStream = new FileStream(rutaCompletaArchivo, FileMode.Create))
                    {
                        await model.Firma.CopyToAsync(fileStream);
                    }

                    // Se guarda la RUTA RELATIVA que irá a la base de datos (ej: /firmas/archivo.jpg)
                    rutaFirma = Path.Combine("/firmas/", nombreArchivoUnico).Replace('\\', '/');
                }

                // 4. CREACIÓN DEL OBJETO USUARIO PARA LA BASE DE DATOS
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
                    repeat_password = model.Repeat_Password, // ¡Considera no guardar esto en texto plano!
                    idRol = model.RolId,
                    ID_gerencia = model.IdGerencia,
                    firma = rutaFirma, 
                    ResponsableFirma = model.ResponsableFirma 
                };

                // 5. LLAMADA AL REPOSITORIO PARA GUARDAR EL USUARIO
                int idNuevoUsuario = await _repositorioUsuarios.RegistrarUsuario(nuevoUsuario);

                TempData["SuccessMessage"] = "Usuario registrado correctamente.";
                return RedirectToAction("ListaUsuarios");
            }
            catch (Exception ex)
            {
                // 6. MANEJO DE ERRORES
                TempData["ErrorMessage"] = $"Error al registrar el usuario: {ex.Message}";
                model.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                model.GerenciasDisponibles = (await _repositorioConfiguracion.ObtenerGerencias())
                                                 .Select(g => new SelectListItem { Value = g.IdGerencia.ToString(), Text = g.Nom_Gerencia }).ToList();
                return View(model);
            }
        }

        [HttpGet]
        [Monitoreo("VerificarCorreoPasoActivos", "SELECT", "verificarCorreoPasoActivos")]
        public async Task<IActionResult> VerificarCorreoPasoActivos(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                // Devuelve un resultado que indique que no se necesita advertencia o es un error de entrada
                return Json(new { requiereAdvertencia = false, error = "Correo no proporcionado" });
            }
            try
            {
                bool existeEnPasoActivos = await _repositorioUsuarios.ExisteCorreoEnPasoActivos(correo);
                // Si existe en paso_activos, no se requiere advertencia.
                // Si NO existe, entonces se requiere advertencia.
                return Json(new { requiereAdvertencia = !existeEnPasoActivos });
            }
            catch (Exception ex)
            {
                // Considera loggear el error ex
                // Devuelve un error o un estado que indique que la verificación falló y quizás no mostrar la advertencia.
                return Json(new { requiereAdvertencia = false, error = "Error al verificar el correo en paso_activos.", detalle = ex.Message });
            }
        }

        [HttpGet]
        [Monitoreo("VerificarCorreoEnAD", "SELECT", "verificarCorreoEnAD")]
        public async Task<IActionResult> VerificarCorreoEnAD(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return Json(new { requiereAdvertencia = false, error = "Correo no proporcionado" });
            }
            try
            {
                bool existeEnAD = await _repositorioUsuarios.ExisteCorreoEnAD(correo);
                // Si existe en AD, NO se requiere advertencia (el correo está autorizado).
                // Si NO existe en AD, entonces SÍ se requiere advertencia.
                return Json(new { requiereAdvertencia = !existeEnAD });
            }
            catch (Exception ex)
            {
                // Considera loggear el error ex
                return Json(new { requiereAdvertencia = false, error = "Error al verificar el correo en AD.", detalle = ex.Message });
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
            // Busca el usuario en la base de datos por su ID.
            var usuario = await _repositorioUsuarios.ObtenerPorId(id);

            // Si no se encuentra el usuario, redirige a la lista con un mensaje de error.
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("ListaUsuarios");
            }

            // Obtiene los datos necesarios para los menús desplegables (roles y gerencias).
            var roles = await _repositorioUsuarios.ObtenerRoles();
            var gerencias = await _repositorioConfiguracion.ObtenerGerencias();

            // Crea el ViewModel (el modelo para la vista) y lo puebla con los datos del usuario.
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
                IdGerencia = usuario.ID_gerencia,
                RolesDisponibles = roles,
                GerenciasDisponibles = gerencias.Select(g => new SelectListItem
                {
                    Value = g.IdGerencia.ToString(),
                    Text = g.Nom_Gerencia
                }),
                // Se cargan los nuevos campos desde el objeto 'usuario' obtenido de la BD.
                ResponsableFirma = usuario.ResponsableFirma,
                FirmaActual = usuario.firma
            };

            // Envía el modelo a la vista para que se muestre el formulario.
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EditarUsuario", "UPDATE", "editarUsuario")]
        public async Task<IActionResult> EditarUsuario(EditarUsuarioVM model)
        {
            // Función auxiliar para recargar los dropdowns en caso de error y tener que volver a mostrar la vista.
            async Task PopulateDropdowns(EditarUsuarioVM m)
            {
                m.RolesDisponibles = await _repositorioUsuarios.ObtenerRoles();
                m.GerenciasDisponibles = (await _repositorioConfiguracion.ObtenerGerencias())
                                         .Select(g => new SelectListItem { Value = g.IdGerencia.ToString(), Text = g.Nom_Gerencia });
            }

            // Si los datos enviados desde el formulario no son válidos (ej. falta un campo requerido),
            // se recargan los dropdowns y se vuelve a mostrar la vista con los mensajes de error.
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            // Se busca el usuario original en la base de datos para actualizarlo.
            var usuarioExistente = await _repositorioUsuarios.ObtenerPorId(model.IdUsuario);
            if (usuarioExistente == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("ListaUsuarios");
            }

            // --- LÓGICA PARA ACTUALIZAR LA FIRMA ---
            // Se comprueba si el usuario ha subido un archivo nuevo en el formulario.
            if (model.NuevaFirma != null && model.NuevaFirma.Length > 0)
            {
                string carpetaDestino = Path.Combine(_webHostEnvironment.WebRootPath, "firmas");
                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }

                // Si ya existía una firma, se elimina el archivo antiguo del servidor para no dejar basura.
                if (!string.IsNullOrEmpty(model.FirmaActual))
                {
                    // Se construye la ruta completa del archivo antiguo.
                    var rutaAntigua = Path.Combine(_webHostEnvironment.WebRootPath, model.FirmaActual.TrimStart('/'));
                    if (System.IO.File.Exists(rutaAntigua))
                    {
                        System.IO.File.Delete(rutaAntigua);
                    }
                }

                // Se guarda el nuevo archivo con un nombre único.
                string nombreArchivoUnico = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.NuevaFirma.FileName);
                string rutaCompletaArchivo = Path.Combine(carpetaDestino, nombreArchivoUnico);
                using (var fileStream = new FileStream(rutaCompletaArchivo, FileMode.Create))
                {
                    await model.NuevaFirma.CopyToAsync(fileStream);
                }

                // Se actualiza la propiedad 'firma' del objeto usuario con la ruta del nuevo archivo.
                usuarioExistente.firma = Path.Combine("/firmas/", nombreArchivoUnico).Replace('\\', '/');
            }
            // Si no se subió un archivo nuevo, no se hace nada y 'usuarioExistente.firma' conserva su valor.

            // Se actualizan las propiedades del usuario con los valores del formulario.
            usuarioExistente.nombre = model.Nombre;
            usuarioExistente.apellidos = model.Apellidos;
            usuarioExistente.rut = model.Rut;
            usuarioExistente.telefono = model.Telefono;
            usuarioExistente.correo = model.Correo;
            usuarioExistente.usuario = model.Usuario;
            usuarioExistente.idRol = model.RolId;
            usuarioExistente.ID_gerencia = model.IdGerencia;
            usuarioExistente.ResponsableFirma = model.ResponsableFirma; // Se actualiza el valor del checkbox.

            // Se llama al repositorio para que ejecute la consulta UPDATE en la base de datos.
            bool actualizado = await _repositorioUsuarios.ActualizarUsuario(usuarioExistente);

            if (actualizado)
            {
                TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                return RedirectToAction("ListaUsuarios");
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar el usuario.");
                await PopulateDropdowns(model);
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


        // PARA GLOSA NEGOCIOS

        private async Task<IEnumerable<PaisNegocioSistemaModel>> GetPnsListAsync()
        {
            if (_cachedPnsList == null)
            {
                _cachedPnsList = await _repositorioRoles.ObtenerPaisNegocioSistemasActivos();
            }
            return _cachedPnsList;
        }

        private async Task PopulateDropdownsForCreateAsync(GlosaNegociosViewModel model)
        {
            var pnsList = await GetPnsListAsync();
            model.PaisesDisponibles = pnsList
                .Where(pns => !string.IsNullOrEmpty(pns.Pais) && pns.IdPais != 0)
                .GroupBy(pns => pns.IdPais)
                .Select(g => g.First())
                .OrderBy(pns => pns.Pais)
                .Select(pns => new SelectListItem
                {
                    Text = pns.Pais,
                    Value = pns.IdPais.ToString(),
                    Selected = model.PaisIdSeleccionado == pns.IdPais.ToString()
                })
                .ToList();

            if (!string.IsNullOrEmpty(model.PaisIdSeleccionado) && int.TryParse(model.PaisIdSeleccionado, out int selectedPaisId) && selectedPaisId != 0)
            {
                var negocios = await _repositorioConfiguracion.ObtenerNegociosPorPaisAsync(selectedPaisId);
                model.NegociosDisponibles = negocios
                    .OrderBy(n => n.Nombre)
                    .Select(n => new SelectListItem
                    {
                        Text = n.Nombre,
                        Value = n.Nombre, // Guardamos el NOMBRE del negocio
                        Selected = model.NegocioNombreSeleccionado == n.Nombre
                    })
                    .ToList();
            }
            else
            {
                model.NegociosDisponibles = new List<SelectListItem> { new SelectListItem { Text = "Seleccione un país primero", Value = "" } };
            }
        }

        private async Task PopulateDropdownsForEditAsync(GlosaNegocioEditViewModel model)
        {
            var pnsList = await GetPnsListAsync();
            model.PaisesDisponibles = pnsList
                .Where(pns => !string.IsNullOrEmpty(pns.Pais) && pns.IdPais != 0)
                .GroupBy(pns => pns.IdPais)
                .Select(g => g.First())
                .OrderBy(pns => pns.Pais)
                .Select(pns => new SelectListItem
                {
                    Text = pns.Pais,
                    Value = pns.IdPais.ToString(),
                    Selected = model.PaisIdSeleccionado == pns.IdPais.ToString()
                })
                .ToList();

            if (!string.IsNullOrEmpty(model.PaisIdSeleccionado) && int.TryParse(model.PaisIdSeleccionado, out int selectedPaisId) && selectedPaisId != 0)
            {
                var negocios = await _repositorioConfiguracion.ObtenerNegociosPorPaisAsync(selectedPaisId);
                model.NegociosDisponibles = negocios
                    .OrderBy(n => n.Nombre)
                    .Select(n => new SelectListItem
                    {
                        Text = n.Nombre,
                        Value = n.Nombre, // Guardamos el NOMBRE del negocio
                        Selected = model.NegocioNombreSeleccionado == n.Nombre
                    })
                    .ToList();
                if (!model.NegociosDisponibles.Any() && selectedPaisId != 0) // Si no hay negocios y se seleccionó un país
                {
                    model.NegociosDisponibles.Insert(0, new SelectListItem { Text = "No hay negocios para este país", Value = "" });
                }
            }
            else
            {
                model.NegociosDisponibles = new List<SelectListItem> { new SelectListItem { Text = "Seleccione un país", Value = "" } };
            }
        }


        [HttpGet]
        [Monitoreo("GlosaNegocios", "SELECT", "verFormularioYListadoGlosas")]
        public async Task<IActionResult> GlosaNegocios()
        {
            var viewModel = new GlosaNegociosViewModel();
            await PopulateDropdownsForCreateAsync(viewModel);
            viewModel.ListadoGlosas = await _repositorioGlosaNegocios.ObtenerTodosAsync();
            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("GlosaNegocios", "INSERT", "crearGlosaNegocio")]
        public async Task<IActionResult> GlosaNegocios(GlosaNegociosViewModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.GlosaParaCrear))
            {
                ModelState.AddModelError(nameof(viewModel.GlosaParaCrear), "La glosa es obligatoria.");
            } // ... (otras validaciones para GlosaParaCrear)

            if (ModelState.IsValid)
            {
                var glosaExistente = await _repositorioGlosaNegocios.ObtenerPorGlosaAsync(viewModel.GlosaParaCrear);
                if (glosaExistente != null)
                {
                    ModelState.AddModelError(nameof(viewModel.GlosaParaCrear), "La glosa '" + viewModel.GlosaParaCrear + "' ya existe.");
                }
                else
                {
                    string nombrePais = null;
                    if (!string.IsNullOrEmpty(viewModel.PaisIdSeleccionado) && int.TryParse(viewModel.PaisIdSeleccionado, out int idPais) && idPais != 0)
                    {
                        var pnsList = await GetPnsListAsync();
                        nombrePais = pnsList.FirstOrDefault(p => p.IdPais == idPais)?.Pais;
                    }

                    var nuevaGlosa = new GlosaNegocioModel
                    {
                        GLOSA = viewModel.GlosaParaCrear,
                        NEGOCIO = string.IsNullOrWhiteSpace(viewModel.NegocioNombreSeleccionado) ? null : viewModel.NegocioNombreSeleccionado,
                        PAIS = nombrePais
                    };
                    await _repositorioGlosaNegocios.CrearAsync(nuevaGlosa);
                    TempData["MensajeExito"] = "Glosa creada exitosamente.";
                    return RedirectToAction(nameof(GlosaNegocios));
                }
            }

            // Si hay errores, repopular dropdowns y lista
            await PopulateDropdownsForCreateAsync(viewModel);
            viewModel.ListadoGlosas = await _repositorioGlosaNegocios.ObtenerTodosAsync();
            return View(viewModel);
        }

        [HttpGet]
        [Monitoreo("CargarFormularioEditarGlosaModal", "SELECT", "cargarFormEditarGlosaModal")]
        public async Task<IActionResult> CargarFormularioEditarGlosaModal(string glosa)
        {
            if (string.IsNullOrEmpty(glosa))
            {
                return BadRequest("La glosa no puede ser nula o vacía.");
            }

            var glosaModel = await _repositorioGlosaNegocios.ObtenerPorGlosaAsync(glosa);
            if (glosaModel == null)
            {
                return NotFound($"No se encontró la glosa: {glosa}");
            }

            var viewModel = new GlosaNegocioEditViewModel
            {
                GlosaOriginal = glosaModel.GLOSA,
                Glosa = glosaModel.GLOSA,
                NegocioNombreSeleccionado = glosaModel.NEGOCIO,
            };

            var pnsList = await GetPnsListAsync();
            var paisEncontrado = pnsList.FirstOrDefault(p => p.Pais == glosaModel.PAIS && p.IdPais != 0);
            if (paisEncontrado != null)
            {
                viewModel.PaisIdSeleccionado = paisEncontrado.IdPais.ToString();
            }

            await PopulateDropdownsForEditAsync(viewModel);

            return PartialView("_EditarGlosaNegociosModalPartial", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EditarGlosaNegociosModal", "UPDATE", "editarGlosaNegocioModal")]
        public async Task<IActionResult> EditarGlosaNegociosModal(GlosaNegocioEditViewModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.GlosaOriginal))
            {
                return Json(new { success = false, errors = new { GlosaOriginal = new[] { "La glosa original es requerida para la actualización." } } });
            }
            if (string.IsNullOrWhiteSpace(viewModel.Glosa))
            {
                ModelState.AddModelError(nameof(viewModel.Glosa), "La glosa es obligatoria.");
            }

            var glosaExistenteConNuevaDefinicion = await _repositorioGlosaNegocios.ObtenerPorGlosaAsync(viewModel.Glosa);
            if (glosaExistenteConNuevaDefinicion != null && !viewModel.Glosa.Equals(viewModel.GlosaOriginal, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(viewModel.Glosa), $"La glosa '{viewModel.Glosa}' ya existe.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsForEditAsync(viewModel);
                var errorList = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, errors = errorList });
            }

            try
            {
                string nombrePais = null;
                if (!string.IsNullOrEmpty(viewModel.PaisIdSeleccionado) && int.TryParse(viewModel.PaisIdSeleccionado, out int idPais) && idPais != 0)
                {
                    var pnsList = await GetPnsListAsync();
                    nombrePais = pnsList.FirstOrDefault(p => p.IdPais == idPais)?.Pais;
                }

                var glosaActualizar = new GlosaNegocioModel
                {
                    GLOSA = viewModel.Glosa,
                    NEGOCIO = string.IsNullOrWhiteSpace(viewModel.NegocioNombreSeleccionado) ? null : viewModel.NegocioNombreSeleccionado,
                    PAIS = nombrePais
                };

                bool actualizado = await _repositorioGlosaNegocios.ActualizarAsync(viewModel.GlosaOriginal, glosaActualizar);

                if (actualizado)
                {
                    return Json(new { success = true, message = "Glosa actualizada exitosamente." });
                }
                else
                {
                    return Json(new { success = false, errors = new { _General_ = new[] { "No se pudo actualizar la glosa." } } });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new { _General_ = new[] { $"Error al actualizar la glosa: {ex.Message}" } } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("EliminarGlosaNegocios", "DELETE", "eliminarGlosaNegocio")]
        public async Task<IActionResult> EliminarGlosaNegocios(string glosa)
        {
            if (string.IsNullOrEmpty(glosa))
            {
                TempData["MensajeError"] = "No se proporcionó la glosa a eliminar.";
                return RedirectToAction(nameof(GlosaNegocios));
            }

            try
            {
                var glosaExistente = await _repositorioGlosaNegocios.ObtenerPorGlosaAsync(glosa);
                if (glosaExistente == null)
                {
                    TempData["MensajeError"] = "La glosa que intenta eliminar no existe.";
                    return RedirectToAction(nameof(GlosaNegocios));
                }

                bool eliminado = await _repositorioGlosaNegocios.EliminarAsync(glosa);
                if (eliminado)
                {
                    TempData["MensajeExito"] = $"Glosa \"{glosa}\" eliminada exitosamente.";
                }
                else
                {
                    TempData["MensajeError"] = $"Error al eliminar la glosa \"{glosa}\".";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al procesar la solicitud de eliminación: {ex.Message}";
            }
            return RedirectToAction(nameof(GlosaNegocios));
        }

        [HttpGet]
        [Monitoreo("NegociosPorPais", "SELECT", "obtenerNegociosFiltradosPorPais")]
        public async Task<JsonResult> ObtenerNegociosFiltradosPorPais(int idPais)
        {
            if (idPais == 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Text = "Seleccione un país primero", Value = "" } });
            }

            var negocios = await _repositorioConfiguracion.ObtenerNegociosPorPaisAsync(idPais);
            var selectList = negocios
                .OrderBy(n => n.Nombre)
                .Select(n => new SelectListItem
                {
                    Text = n.Nombre,
                    Value = n.Nombre // El valor es el nombre del negocio
                }).ToList();

            if (!selectList.Any())
            {
                selectList.Insert(0, new SelectListItem { Text = "No hay negocios para este país", Value = "" });
            }
            else
            {
                selectList.Insert(0, new SelectListItem { Text = "Seleccione Negocio (Opcional)", Value = "" });
            }


            return Json(selectList);
        }
    }
}