using ABM.Models;
using ABM.Filters;
using ABM.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ABM.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly IRepositorioConfiguracion _repo;
        public ConfiguracionController(IRepositorioConfiguracion repo)
            => _repo = repo;

        // GET: /Configuracion/Paises
        [HttpGet]
        [Monitoreo("Paises", "SELECT", "verListadoPaises")]
        public async Task<IActionResult> Paises()
        {
            var lista = await _repo.ObtenerPaises();
            return View("Paises", lista);
        }

        // POST: /Configuracion/CrearPais
        [HttpPost]
        [Monitoreo("Paises", "INSERT", "crearPais")]
        public async Task<IActionResult> CrearPais(Pais modelo, IFormFile? BanderaFile)
        {
            // 1) Validaciones de duplicados
            if (await _repo.ExistePaisCodigo(modelo.CodPais))
                ModelState.AddModelError("CodPais", "El código de Pais ya existe.");
            if (await _repo.ExistePaisNombre(modelo.Nombre))
                ModelState.AddModelError("Nombre", "El nombre del Pais ya existe.");

            // 2) Si hay errores, volver a cargar la vista con ModelState
            if (!ModelState.IsValid)
            {
                var lista = await _repo.ObtenerPaises();
                return View("Paises", lista);
            }

            // 3) Procesar imagen
            if (BanderaFile != null)
            {
                using var ms = new MemoryStream();
                await BanderaFile.CopyToAsync(ms);
                modelo.Bandera = Convert.ToBase64String(ms.ToArray());
            }

            await _repo.CrearPais(modelo);
            return RedirectToAction(nameof(Paises));
        }

        // GET: /Configuracion/EditarPais?id=#
        [HttpGet]
        [Monitoreo("EditarPais", "SELECT", "obtenerPaisPorId")]
        public async Task<IActionResult> EditarPais(int id)
        {
            var pais = await _repo.ObtenerPaisPorId(id);
            if (pais == null) return NotFound();
            return Json(pais);
        }

        // POST: /Configuracion/EditarPais
        [HttpPost]
        [Monitoreo("EditarPais", "UPDATE", "editarPais")]
        public async Task<IActionResult> EditarPais(Pais modelo, IFormFile? BanderaFile)
        {
            // 1) Validaciones de duplicados
            if (await _repo.ExistePaisCodigo(modelo.CodPais, modelo.IdPais))
                ModelState.AddModelError("CodPais", "El código ya existe.");
            if (await _repo.ExistePaisNombre(modelo.Nombre, modelo.IdPais))
                ModelState.AddModelError("Nombre", "El nombre ya existe.");

            // 2) Si hay errores: devolvemos JSON con los mensajes para el modal
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .ToDictionary(
                       kvp => kvp.Key,
                       kvp => kvp.Value.Errors.First().ErrorMessage
                    );
                return BadRequest(errors);
            }

            // 3) Procesar imagen (o conservar la anterior)
            if (BanderaFile != null)
            {
                using var ms = new MemoryStream();
                await BanderaFile.CopyToAsync(ms);
                modelo.Bandera = Convert.ToBase64String(ms.ToArray());
            }
            else
            {
                var existente = await _repo.ObtenerPaisPorId(modelo.IdPais);
                modelo.Bandera = existente?.Bandera;
            }

            await _repo.ActualizarPais(modelo);
            return Ok();
        }
        //-----------------------------CONTROLLER PARA LOS SISTEMAS ---------------------------------------------

        // GET: /Configuracion/Sistemas
        [HttpGet]
        [Monitoreo("Sistemas", "SELECT", "verListadoSistemas")]
        public async Task<IActionResult> Sistemas()
        {
            var lista = await _repo.ObtenerSistemas();
            return View("Sistemas", lista);
        }

        // POST: /Configuracion/CrearSistema
        [HttpPost]
        [Monitoreo("Sistemas", "INSERT", "crearSistema")]
        public async Task<IActionResult> CrearSistema(Sistema modelo)
        {
            if (await _repo.ExisteSistemaCodigo(modelo.codSistema))
                ModelState.AddModelError(nameof(modelo.codSistema), "El código ya existe.");
            if (await _repo.ExisteSistemaNombre(modelo.sistema))
                ModelState.AddModelError(nameof(modelo.sistema), "El nombre ya existe.");

            if (!ModelState.IsValid)
                return View("Sistemas", await _repo.ObtenerSistemas());

            await _repo.CrearSistema(modelo);
            return RedirectToAction(nameof(Sistemas));
        }

        // GET: /Configuracion/EditarSistema?id=#
        [HttpGet]
        [Monitoreo("EditarSistema", "SELECT", "obtenerSistemaPorId")]
        public async Task<IActionResult> EditarSistema(int id)
        {
            var sis = await _repo.ObtenerSistemaPorId(id);
            if (sis == null) return NotFound();
            return Json(sis);
        }

        // POST: /Configuracion/EditarSistema
        [HttpPost]
        [Monitoreo("EditarSistema", "UPDATE", "editarSistema")]
        public async Task<IActionResult> EditarSistema(Sistema modelo)
        {
            if (await _repo.ExisteSistemaCodigo(modelo.codSistema, modelo.idSistema))
                ModelState.AddModelError(nameof(modelo.codSistema), "El código ya existe.");
            if (await _repo.ExisteSistemaNombre(modelo.sistema, modelo.idSistema))
                ModelState.AddModelError(nameof(modelo.sistema), "El nombre ya existe.");

            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );
                return BadRequest(errores);
            }

            await _repo.ActualizarSistema(modelo);
            return Ok();
        }

        //-----------------------------CONTROLLER PARA LOS NEGOCIOS ---------------------------------------------

        // GET: /Configuracion/Negocios
        [HttpGet]
        [Monitoreo("Negocios", "SELECT", "verListadoNegocios")]
        public async Task<IActionResult> Negocios()
        {
            var lista = await _repo.ObtenerNegocios();
            return View("Negocios", lista);
        }

        // POST: /Configuracion/CrearNegocio
        [HttpPost]
        [Monitoreo("Negocios", "INSERT", "crearNegocio")]
        public async Task<IActionResult> CrearNegocio(Negocio modelo) // El modelo es Negocio
        {
            // Solo validamos por nombre para Negocio
            if (await _repo.ExisteNegocioNombre(modelo.Nombre)) // Usar modelo.Nombre
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre del negocio ya existe.");

            if (!ModelState.IsValid)
            {
                // Repopular el valor del formulario para que el usuario no lo pierda
                ViewData["NombreNegocioValue"] = modelo.Nombre;
                return View("Negocios", await _repo.ObtenerNegocios());
            }

            await _repo.CrearNegocio(modelo);
            return RedirectToAction(nameof(Negocios));
        }

        // GET: /Configuracion/EditarNegocio?id=#
        [HttpGet]
        [Monitoreo("EditarNegocio", "SELECT", "obtenerNegocioPorId")]
        public async Task<IActionResult> EditarNegocio(int id) // Recibe id
        {
            var negocio = await _repo.ObtenerNegocioPorId(id);
            if (negocio == null) return NotFound();
            return Json(negocio); // Devuelve el objeto Negocio
        }

        // POST: /Configuracion/EditarNegocio
        [HttpPost]
        [Monitoreo("EditarNegocio", "UPDATE", "editarNegocio")]
        public async Task<IActionResult> EditarNegocio(Negocio modelo) // Recibe el modelo Negocio
        {
            // Validamos por nombre, excluyendo el ID actual
            if (await _repo.ExisteNegocioNombre(modelo.Nombre, modelo.IdNegocio))
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre del negocio ya existe.");

            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key, // La clave será "Nombre" (del modelo Negocio)
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );
                return BadRequest(errores);
            }

            await _repo.ActualizarNegocio(modelo);
            return Ok();
        }

        // --- MÉTODOS PARA CRUCE PAIS-NEGOCIO-SISTEMA (PNS) ---

        [HttpGet]
        [Monitoreo("CrucesPNS", "SELECT_PAGE", "verPaginaCrucesPNS")]
        public async Task<IActionResult> CrucesPNS()
        {
            var listaCruces = await _repo.ObtenerCrucesPNS();
            if (listaCruces == null)
            {
                listaCruces = new List<CrucePNSViewModel>(); // Asegurar que no sea nulo para la vista
            }

            var paises = await _repo.ObtenerPaises() ?? new List<Pais>();
            var negocios = await _repo.ObtenerNegocios() ?? new List<Negocio>();
            var sistemas = await _repo.ObtenerSistemas() ?? new List<Sistema>();

            var cruceParaCrearForm = new CrucePNSViewModel
            {
                Paises = new SelectList(paises, nameof(Pais.IdPais), nameof(Pais.Nombre)),
                Negocios = new SelectList(negocios, nameof(Negocio.IdNegocio), nameof(Negocio.Nombre)),
                Sistemas = new SelectList(sistemas, "idSistema", "sistema")
            };

            var pageViewModel = new CrucesPNSPageViewModel
            {
                CrucesList = listaCruces,
                CruceParaCrear = cruceParaCrearForm
            };
            return View(pageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("CrucePNS", "INSERT", "ejecutarCreacionCrucePNS")]
        public async Task<IActionResult> CrearCrucePNS(CrucesPNSPageViewModel pageModel) // Cambiado el parámetro
        {
            // Extraer el modelo del cruce desde el pageModel
            var modeloForm = pageModel?.CruceParaCrear;

            if (modeloForm == null)
            {
                TempData["ErrorMessage"] = "No se recibieron datos válidos para procesar el formulario.";
                // Redirigir o preparar un PageViewModel vacío para la vista es mejor que null
                var emptyPageViewModel = new CrucesPNSPageViewModel
                {
                    CrucesList = await _repo.ObtenerCrucesPNS() ?? new List<CrucePNSViewModel>(),
                    CruceParaCrear = new CrucePNSViewModel // Modelo vacío para el formulario
                    {
                        Paises = new SelectList(await _repo.ObtenerPaises() ?? new List<Pais>(), nameof(Pais.IdPais), nameof(Pais.Nombre)),
                        Negocios = new SelectList(await _repo.ObtenerNegocios() ?? new List<Negocio>(), nameof(Negocio.IdNegocio), nameof(Negocio.Nombre)),
                        Sistemas = new SelectList(await _repo.ObtenerSistemas() ?? new List<Sistema>(), "idSistema", "sistema")
                    }
                };
                return View("CrucesPNS", emptyPageViewModel);
            }

            // La validación de ExistenciaCruce ahora usa los datos de modeloForm
            if (await _repo.ExisteCrucePNS(modeloForm.IdPais, modeloForm.IdNegocio, modeloForm.IdSistema))
            {
                ModelState.AddModelError("CruceParaCrear.IdPais", $"La combinación País, Negocio y Sistema ya existe.");
            }

            // ModelState.IsValid ahora se evalúa sobre el 'pageModel' completo.
            // Si las DataAnnotations están en CrucePNSViewModel, los errores estarán anidados bajo "CruceParaCrear.Propiedad".
            if (!ModelState.IsValid)
            {
                // El modeloForm ya tiene los valores enviados por el usuario.
                // Solo necesitamos repoblar las listas de selección y la lista de cruces.
                var listaCruces = await _repo.ObtenerCrucesPNS() ?? new List<CrucePNSViewModel>();
                var paises = await _repo.ObtenerPaises() ?? new List<Pais>();
                var negocios = await _repo.ObtenerNegocios() ?? new List<Negocio>();
                var sistemas = await _repo.ObtenerSistemas() ?? new List<Sistema>();

                modeloForm.Paises = new SelectList(paises, nameof(Pais.IdPais), nameof(Pais.Nombre), modeloForm.IdPais);
                modeloForm.Negocios = new SelectList(negocios, nameof(Negocio.IdNegocio), nameof(Negocio.Nombre), modeloForm.IdNegocio);
                modeloForm.Sistemas = new SelectList(sistemas, "idSistema", "sistema", modeloForm.IdSistema);

                // Reconstruimos el pageModel para la vista, manteniendo el modeloForm con los errores.
                var viewModelParaVista = new CrucesPNSPageViewModel
                {
                    CrucesList = listaCruces,
                    CruceParaCrear = modeloForm // modeloForm ya es parte de pageModel, pero lo asignamos explícitamente para claridad
                };
                TempData["ErrorMessage"] = "No se pudo crear el Cruce PNS. Por favor, corrija los errores e intente nuevamente.";
                return View("CrucesPNS", viewModelParaVista);
            }

            // Si ModelState.IsValid es true, procedemos con la creación.
            var pns = new PaisNegocioSistema
            {
                IdPais = modeloForm.IdPais,
                IdNegocio = modeloForm.IdNegocio,
                IdSistema = modeloForm.IdSistema,
                Estado = "1"
            };

            int nuevoIdPaisNegocioSistema = 0;
            try
            {
                nuevoIdPaisNegocioSistema = await _repo.CrearPaisNegocioSistema(pns);
            }
            catch (Exception ex)
            {
                // Loggear ex para más detalles (ex.ToString() para stack trace completo)
                ModelState.AddModelError(string.Empty, $"Error crítico al guardar el cruce principal: {ex.Message}. Revise los logs del servidor.");
            }

            if (nuevoIdPaisNegocioSistema > 0)
            {
                var pnsjt = new Pnsjt
                {
                    Tabla = modeloForm.Tabla,
                    Trans = modeloForm.Trans,
                    IdPaisNegocioSistema = nuevoIdPaisNegocioSistema,
                    Ip = modeloForm.Ip,
                    Responsable = modeloForm.Responsable
                };
                try
                {
                    await _repo.CrearPnsjt(pnsjt);
                    TempData["MensajeExito"] = "Cruce PNS creado exitosamente.";
                    return RedirectToAction(nameof(CrucesPNS));
                }
                catch (Exception exPnsjt)
                {
                    // Loggear exPnsjt para más detalles
                    // Considerar lógica de rollback para 'pns' si esta parte falla.
                    ModelState.AddModelError(string.Empty, $"Se creó el cruce principal, pero hubo un error al guardar los detalles (PNSJT): {exPnsjt.Message}. Contacte a soporte.");
                }
            }
            else if (ModelState.ErrorCount == 0) // Si no es > 0 Y NO se añadieron errores por excepción
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el ID del nuevo cruce principal (el repositorio devolvió un ID no válido).");
            }

            // --- Fallback si algo falló después de la validación inicial ---
            // ModelState ya debería contener los errores específicos.
            // Repopulamos el PageViewModel completo para devolverlo a la vista.
            var fallbackListaCruces = await _repo.ObtenerCrucesPNS() ?? new List<CrucePNSViewModel>();
            var fallbackPaises = await _repo.ObtenerPaises() ?? new List<Pais>();
            var fallbackNegocios = await _repo.ObtenerNegocios() ?? new List<Negocio>();
            var fallbackSistemas = await _repo.ObtenerSistemas() ?? new List<Sistema>();

            modeloForm.Paises = new SelectList(fallbackPaises, nameof(Pais.IdPais), nameof(Pais.Nombre), modeloForm.IdPais);
            modeloForm.Negocios = new SelectList(fallbackNegocios, nameof(Negocio.IdNegocio), nameof(Negocio.Nombre), modeloForm.IdNegocio);
            modeloForm.Sistemas = new SelectList(fallbackSistemas, "idSistema", "sistema", modeloForm.IdSistema);

            var fallbackPageViewModel = new CrucesPNSPageViewModel
            {
                CrucesList = fallbackListaCruces,
                CruceParaCrear = modeloForm // modeloForm contiene los valores ingresados y el estado de validación
            };
            TempData["ErrorMessage"] = "Ocurrió un error durante el proceso de creación. Revise los mensajes.";
            return View("CrucesPNS", fallbackPageViewModel);
        }

        [HttpGet]
        [Monitoreo("CrucePNS", "GET_EDIT_DATA", "obtenerDatosParaEditarCrucePNS")]
        public async Task<IActionResult> EditarCrucePNS(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "El ID proporcionado no es válido." });
            }

            var modeloCruce = await _repo.ObtenerCrucePNSPorId(id);
            if (modeloCruce == null)
            {
                return Json(new { success = false, message = $"No se encontró el Cruce PNS con ID {id}." });
            }
            return Json(new { success = true, data = modeloCruce });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("CrucePNS", "UPDATE_AJAX", "ejecutarEdicionCrucePNSAjax")]
        public async Task<IActionResult> EditarCrucePNS(int id, CrucePNSViewModel modelo)
        {
            if (modelo == null)
            {
                return BadRequest(new { Message = "No se recibieron datos para actualizar." });
            }
            if (id != modelo.IdPaisNegocioSistema)
            {
                return BadRequest(new { Message = "Discrepancia en el ID del Cruce PNS. No se puede procesar la solicitud." });
            }

            // Validaciones explícitas adicionales a las DataAnnotations
            if (modelo.IdPais <= 0) ModelState.AddModelError(nameof(modelo.IdPais), "Debe seleccionar un País.");
            if (modelo.IdNegocio <= 0) ModelState.AddModelError(nameof(modelo.IdNegocio), "Debe seleccionar un Negocio.");
            if (modelo.IdSistema <= 0) ModelState.AddModelError(nameof(modelo.IdSistema), "Debe seleccionar un Sistema.");
            if (string.IsNullOrWhiteSpace(modelo.Tabla)) ModelState.AddModelError(nameof(modelo.Tabla), "El campo Nombre tabla es obligatorio.");
            if (string.IsNullOrWhiteSpace(modelo.Ip)) ModelState.AddModelError(nameof(modelo.Ip), "El campo IP Máquina es obligatorio.");
            if (string.IsNullOrWhiteSpace(modelo.Responsable)) ModelState.AddModelError(nameof(modelo.Responsable), "El campo Responsable es obligatorio.");


            if (modelo.IdPais > 0 && modelo.IdNegocio > 0 && modelo.IdSistema > 0)
            {
                if (await _repo.ExisteCrucePNS(modelo.IdPais, modelo.IdNegocio, modelo.IdSistema, modelo.IdPaisNegocioSistema))
                {
                    ModelState.AddModelError("IdSistema", $"La combinación País, Negocio y Sistema ya existe para otro registro.");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var pnsExistente = await _repo.ObtenerPaisNegocioSistemaPorId(modelo.IdPaisNegocioSistema);
            if (pnsExistente == null)
            {
                return NotFound(new { Message = $"El registro base del cruce (ID: {modelo.IdPaisNegocioSistema}) no fue encontrado y no se puede actualizar." });
            }

            // Actualizar campos permitidos, Estado se preserva desde pnsExistente.Estado
            pnsExistente.IdPais = modelo.IdPais;
            pnsExistente.IdNegocio = modelo.IdNegocio;
            pnsExistente.IdSistema = modelo.IdSistema;

            bool pnsActualizado = false;
            try
            {
                pnsActualizado = await _repo.ActualizarPaisNegocioSistema(pnsExistente);
            }
            catch (Exception exPns)
            {
                // Log exPns
                return StatusCode(500, new { Message = $"Error al actualizar el cruce principal: {exPns.Message}" });
            }


            bool pnsjtOperacionExitosa = false;
            var pnsjtExistente = await _repo.ObtenerPnsjtPorIdPaisNegocioSistema(modelo.IdPaisNegocioSistema);

            try
            {
                if (pnsjtExistente != null) // Si existe ftc_pnsjt, se actualiza
                {
                    pnsjtExistente.Tabla = modelo.Tabla;
                    pnsjtExistente.Trans = modelo.Trans;
                    pnsjtExistente.Ip = modelo.Ip;
                    pnsjtExistente.Responsable = modelo.Responsable;
                    pnsjtOperacionExitosa = await _repo.ActualizarPnsjt(pnsjtExistente);
                }
                else // Si no existe ftc_pnsjt, se crea
                {
                    var pnsjtToCreate = new Pnsjt
                    {
                        IdPaisNegocioSistema = modelo.IdPaisNegocioSistema,
                        Tabla = modelo.Tabla,
                        Trans = modelo.Trans,
                        Ip = modelo.Ip,
                        Responsable = modelo.Responsable
                    };
                    await _repo.CrearPnsjt(pnsjtToCreate);
                    pnsjtOperacionExitosa = true;
                }
            }
            catch (Exception exPnsjt)
            {
                // Log exPnsjt
                // Si pnsActualizado fue true pero esto falla, el estado es inconsistente.
                // Idealmente, usar transacciones en el repositorio para estas operaciones combinadas.
                return StatusCode(500, new { Message = $"Error al actualizar/crear los detalles del cruce (PNSJT): {exPnsjt.Message}" });
            }


            if (pnsActualizado || pnsjtOperacionExitosa)
            {
                return Ok(new { Message = "Cruce PNS actualizado exitosamente." });
            }
            else
            {
                return Ok(new { Message = "No se detectaron cambios para actualizar o la operación no afectó registros." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Importante para la seguridad en operaciones POST
        [Monitoreo("CrucePNS", "UPDATE_ESTADO", "deshabilitarCrucePNS")]
        public async Task<IActionResult> DeshabilitarCruce(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "El ID del cruce proporcionado no es válido.";
                return RedirectToAction(nameof(CrucesPNS));
            }

            try
            {
                bool deshabilitadoConExito = await _repo.DeshabilitarCrucePNS(id);

                if (deshabilitadoConExito)
                {
                    TempData["MensajeExito"] = $"El Cruce PNS (ID: {id}) ha sido deshabilitado correctamente.";
                }
                else
                {
                    // Esto puede ocurrir si el cruce no existía, ya estaba deshabilitado, o hubo un error no esperado.
                    TempData["ErrorMessage"] = $"No se pudo deshabilitar el Cruce PNS (ID: {id}). Es posible que ya estuviera deshabilitado o no se encontrara.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar deshabilitar el cruce. Por favor, contacte a soporte.";
            }

            return RedirectToAction(nameof(CrucesPNS));
        }

        [HttpGet]
        public async Task<JsonResult> GetNegociosPorPais(int idPais)
        {
            if (idPais <= 0)
            {
                // Devuelve una lista vacía si el idPais no es válido para evitar errores en el JS
                return Json(new List<SelectListItem>());
            }
            var negocios = await _repo.ObtenerNegociosPorPaisAsync(idPais);
            // Mapea la lista de Negocio a una lista de SelectListItem
            // Asegúrate que las propiedades IdNegocio y Nombre existan en tu clase Negocio
            var selectListItems = negocios
                                    .Select(n => new SelectListItem { Value = n.IdNegocio.ToString(), Text = n.Nombre })
                                    .ToList();
            return Json(selectListItems);
        }

        [HttpGet]
        public async Task<JsonResult> GetSistemasPorPaisYNegocio(int idPais, int idNegocio)
        {
            if (idPais <= 0 || idNegocio <= 0)
            {
                // Devuelve una lista vacía si los IDs no son válidos
                return Json(new List<SelectListItem>());
            }
            var sistemas = await _repo.ObtenerSistemasPorPaisYNegocioAsync(idPais, idNegocio);
            // Mapea la lista de Sistema a una lista de SelectListItem
            // Asegúrate que las propiedades idSistema y sistema existan en tu clase Sistema
            var selectListItems = sistemas
                                    .Select(s => new SelectListItem { Value = s.idSistema.ToString(), Text = s.sistema })
                                    .ToList();
            return Json(selectListItems);
        }


        [HttpGet]
        // [Monitoreo("GestionCorreos", "GET_EDIT_FORM_DATA", "obtenerDatosParaEditarCorreo")]
        public async Task<IActionResult> GetDatosEnvioCorreo(int id) // Renombrado para claridad
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID no válido." });
            }
            var viewModel = await _repo.ObtenerEnvioCorreoDetalleVMPorIdAsync(id);
            if (viewModel == null)
            {
                return Json(new { success = false, message = "Configuración de correo no encontrada." });
            }
            return Json(new { success = true, data = viewModel });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Monitoreo("GestionCorreos", "UPDATE_AJAX", "ejecutarEdicionCorreoDetalle")]
        public async Task<IActionResult> EditarEnvioCorreo(int id, EnvioCorreoDetalleViewModel modeloForm) // Recibe el ViewModel
        {
            if (modeloForm == null || id != modeloForm.IdCorreos)
            {
                return BadRequest(new { Message = "Datos inválidos o discrepancia de ID." });
            }


            int? idPNSActualizado = await _repo.ObtenerIdPaisNegocioSistemaActivoAsync(modeloForm.IdPais, modeloForm.IdNegocio, modeloForm.IdSistema);

            if (idPNSActualizado == null)
            {
                ModelState.AddModelError("IdSistema", "La combinación de País, Negocio y Sistema seleccionada para la actualización no existe o no está activa.");
                // El nombre de la clave "IdSistema" es un ejemplo; ajústalo al campo del modal que corresponda.
            }


            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Devuelve errores de validación
            }

            var entidadParaActualizar = new EnvioCorreoDetalle
            {
                IdCorreos = modeloForm.IdCorreos,
                IdPaisNegocioSistema = idPNSActualizado.Value, // Usar el idPNS validado/actualizado
                OSI = modeloForm.OSI,
                Correo_OSI = modeloForm.Correo_OSI,
                Responsable = modeloForm.Responsable,
                Correo_Responsable = modeloForm.Correo_Responsable,
                Gerente = modeloForm.Gerente,
                Correo_Gerente = modeloForm.Correo_Gerente,
                Jefe = modeloForm.Jefe,
                Correo_Jefe = modeloForm.Correo_Jefe,
                Otros_Correos = modeloForm.Otros_Correos
            };

            try
            {
                bool actualizado = await _repo.ActualizarEnvioCorreoDetalleAsync(entidadParaActualizar);
                if (actualizado)
                {
                    return Ok(new { Message = "Configuración de correo actualizada exitosamente." });
                }
                else
                {
                    // Podría ser que no se encontrara el ID o que los datos fueran idénticos y no se afectaran filas.
                    return Ok(new { Message = "No se realizaron cambios. Verifique los datos o el registro no fue encontrado para actualizar." });
                }
            }
            catch (Exception ex)
            {
                // Log ex.ToString()
                return StatusCode(500, new { Message = $"Error crítico al actualizar: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
         [Monitoreo("GestionCorreos", "DELETE", "eliminarConfiguracionCorreo")] 
        public async Task<IActionResult> EliminarEnvioCorreo(int id) 
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "El ID proporcionado no es válido." });
            }

            try
            {

                bool exito = await _repo.EliminarEnvioCorreoDetalleAsync(id);

                if (exito)
                {
                    return Json(new { success = true, message = "Configuración de correo eliminada exitosamente." });
                }
                else
                {
                    // Esto puede ocurrir si el ID no se encontró o si la eliminación no afectó filas.
                    return Json(new { success = false, message = "No se pudo eliminar la configuración de correo. Es posible que ya haya sido eliminada o no exista." });
                }
            }
            catch (SqlException sqlEx) 
            {

                return Json(new { success = false, message = "Error de base de datos al intentar eliminar la configuración. Verifique si existen datos relacionados." });
            }
            catch (Exception ex)
            {
                // Loguear ex.ToString() para detalles.
                return Json(new { success = false, message = "Ocurrió un error inesperado al procesar la solicitud de eliminación." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GestionCorreos()
        {
            var pageViewModel = new GestionCorreosPageViewModel();
            pageViewModel.ListaCorreos = await _repo.ObtenerEnvioCorreoDetallesVMAsync() ?? new List<EnvioCorreoDetalleViewModel>();

            var paises = await _repo.ObtenerPaises() ?? new List<Pais>();
            pageViewModel.CorreoParaCrear.Paises = new SelectList(paises, nameof(Pais.IdPais), nameof(Pais.Nombre));

            // Inicializar Negocios y Sistemas como listas vacías de SelectListItem
            pageViewModel.CorreoParaCrear.Negocios = new List<SelectListItem> { new SelectListItem("Seleccione Negocio...", "") };
            pageViewModel.CorreoParaCrear.Sistemas = new List<SelectListItem> { new SelectListItem("Seleccione Sistema...", "") };

            return View(pageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEnvioCorreo(GestionCorreosPageViewModel pageModel)
        {
            var modeloForm = pageModel?.CorreoParaCrear;

            if (modeloForm == null)
            {
                TempData["ErrorMessage"] = "Datos del formulario no válidos.";
                await RepopulateGestionCorreosViewModelForCreateError(pageModel ?? new GestionCorreosPageViewModel());
                return View("GestionCorreos", pageModel ?? new GestionCorreosPageViewModel());
            }

            // Validaciones básicas para los selects
            if (modeloForm.IdPais == 0) ModelState.AddModelError("CorreoParaCrear.IdPais", "Debe seleccionar un País.");
            if (modeloForm.IdNegocio == 0) ModelState.AddModelError("CorreoParaCrear.IdNegocio", "Debe seleccionar un Negocio.");
            if (modeloForm.IdSistema == 0) ModelState.AddModelError("CorreoParaCrear.IdSistema", "Debe seleccionar un Sistema.");

            int? idPNS = null;
            string nombrePais = "N/A", nombreNegocio = "N/A", nombreSistema = "N/A";

            if (modeloForm.IdPais > 0 && modeloForm.IdNegocio > 0 && modeloForm.IdSistema > 0)
            {
                idPNS = await _repo.ObtenerIdPaisNegocioSistemaActivoAsync(modeloForm.IdPais, modeloForm.IdNegocio, modeloForm.IdSistema);
                if (idPNS == null)
                {
                    ModelState.AddModelError("CorreoParaCrear.IdSistema", "La combinación de País, Negocio y Sistema seleccionada no existe o no está activa.");
                }
                else
                {
                    // Obtener nombres para NombreLista
                    var paisObj = (await _repo.ObtenerPaises()).FirstOrDefault(p => p.IdPais == modeloForm.IdPais);
                    var negocioObj = (await _repo.ObtenerNegociosPorPaisAsync(modeloForm.IdPais)).FirstOrDefault(n => n.IdNegocio == modeloForm.IdNegocio); // Asume que ObtenerNegociosPorPaisAsync existe y es adecuado
                    var sistemaObj = (await _repo.ObtenerSistemasPorPaisYNegocioAsync(modeloForm.IdPais, modeloForm.IdNegocio)).FirstOrDefault(s => s.idSistema == modeloForm.IdSistema); // Asume que ObtenerSistemasPorPaisYNegocioAsync existe

                    nombrePais = paisObj?.Nombre ?? $"ID{modeloForm.IdPais}";
                    nombreNegocio = negocioObj?.Nombre ?? $"ID{modeloForm.IdNegocio}";
                    nombreSistema = sistemaObj?.sistema ?? $"ID{modeloForm.IdSistema}";
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor corrija los errores indicados.";
                await RepopulateGestionCorreosViewModelForCreateError(pageModel);
                return View("GestionCorreos", pageModel);
            }

            int nuevoIdCorreo = await _repo.ObtenerSiguienteIdCorreosAsync();

            var nuevoEnvioCorreoDetalle = new EnvioCorreoDetalle
            {
                IdCorreos = nuevoIdCorreo,
                IdPaisNegocioSistema = idPNS.Value, // Ya validado que no es null si ModelState es válido
                OSI = modeloForm.OSI,
                Correo_OSI = modeloForm.Correo_OSI,
                Responsable = modeloForm.Responsable,
                Correo_Responsable = modeloForm.Correo_Responsable,
                Gerente = modeloForm.Gerente,
                Correo_Gerente = modeloForm.Correo_Gerente,
                Jefe = modeloForm.Jefe,
                Correo_Jefe = modeloForm.Correo_Jefe,
                Otros_Correos = modeloForm.Otros_Correos
            };

            // Para ftc_Envio_Correo_lista
            string nombreListaConcatenado = $"{nombrePais} - {nombreNegocio} - {nombreSistema}";
            if (nombreListaConcatenado.Length > 100) nombreListaConcatenado = nombreListaConcatenado.Substring(0, 100);

            var nuevoEnvioCorreoLista = new EnvioCorreoLista
            {
                // IdCorreos = nuevoIdCorreo, // NO ESTABLECER ESTO - Es IDENTITY
                IdDetalleCorreo = nuevoIdCorreo, // ESTABLECER LA NUEVA FK

                NombreLista = nombreListaConcatenado,
                Envio_Diario = modeloForm.CheckEnvioDiario ? "si" : "no",
                Envio_Semanal = modeloForm.CheckEnvioSemanal ? "si" : "no",
                Envio_Gerente = modeloForm.CheckEnvioGerente ? "si" : "no",
                Envio_Mensual = modeloForm.CheckEnvioMensual ? "si" : "no",
                Envio_Quincenal = modeloForm.CheckEnvioQuincenal ? "si" : "no",
                Envio_Jefe = modeloForm.CheckEnvioJefe ? "si" : "no",
                Tipo_Carga = "AUTOMATICA",
                Fecha_Ultima_Carga = DateTime.Now
            };

            // **Importante: Considerar Transacciones**
            // Si una inserción falla, la otra debería revertirse.
            // Dapper requiere manejo manual de transacciones.
            // Por simplicidad, aquí se hacen secuenciales.
            try
            {
                bool detalleCreado = await _repo.CrearEnvioCorreoDetalleAsync(nuevoEnvioCorreoDetalle);
                if (detalleCreado)
                {
                    bool listaCreada = await _repo.CrearEnvioCorreoListaAsync(nuevoEnvioCorreoLista);
                    if (listaCreada)
                    {
                        TempData["MensajeExito"] = $"Gestion de correo y lista asociada creadas exitosamente.";
                        return RedirectToAction(nameof(GestionCorreos));
                    }
                    else
                    {
                        // Detalle se creó, pero lista falló. ¡INCONSISTENCIA!
                        // Aquí deberías idealmente revertir la creación del detalle.
                        TempData["ErrorMessage"] = $"Se creó el detalle del correo (ID: {nuevoIdCorreo}) pero falló la creación de la lista de envío. Por favor, revise o contacte a soporte.";
                        // Podrías intentar eliminar el detalle aquí o marcarlo para revisión.
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo registrar el detalle de la configuración de correos.";
                }
            }
            catch (Exception ex)
            {
                // Log ex.ToString()
                TempData["ErrorMessage"] = $"Error crítico al guardar: {ex.Message}";
            }

            // Si algo falla, repopular y volver a la vista
            await RepopulateGestionCorreosViewModelForCreateError(pageModel);
            return View("GestionCorreos", pageModel);
        }

        // Método helper para repopular el ViewModel en caso de error al crear
        private async Task RepopulateGestionCorreosViewModelForCreateError(GestionCorreosPageViewModel pageModel)
        {
            pageModel.CorreoParaCrear = pageModel.CorreoParaCrear ?? new EnvioCorreoDetalleViewModel(); // Asegurar que no sea null

            pageModel.ListaCorreos = await _repo.ObtenerEnvioCorreoDetallesVMAsync() ?? new List<EnvioCorreoDetalleViewModel>();
            var paises = await _repo.ObtenerPaises() ?? new List<Pais>();
            pageModel.CorreoParaCrear.Paises = new SelectList(paises, nameof(Pais.IdPais), nameof(Pais.Nombre), pageModel.CorreoParaCrear.IdPais);

            // Repoblar Negocios y Sistemas basados en la selección actual, si existe
            if (pageModel.CorreoParaCrear.IdPais > 0)
            {
                var negociosFiltrados = await _repo.ObtenerNegociosPorPaisAsync(pageModel.CorreoParaCrear.IdPais);
                pageModel.CorreoParaCrear.Negocios = new SelectList(negociosFiltrados, nameof(Negocio.IdNegocio), nameof(Negocio.Nombre), pageModel.CorreoParaCrear.IdNegocio);

                if (pageModel.CorreoParaCrear.IdNegocio > 0)
                {
                    var sistemasFiltrados = await _repo.ObtenerSistemasPorPaisYNegocioAsync(pageModel.CorreoParaCrear.IdPais, pageModel.CorreoParaCrear.IdNegocio);
                    pageModel.CorreoParaCrear.Sistemas = new SelectList(sistemasFiltrados, "idSistema", "sistema", pageModel.CorreoParaCrear.IdSistema);
                }
                else
                {
                    pageModel.CorreoParaCrear.Sistemas = new List<SelectListItem> { new SelectListItem("Seleccione Sistema...", "") };
                }
            }
            else
            {
                pageModel.CorreoParaCrear.Negocios = new List<SelectListItem> { new SelectListItem("Seleccione Negocio...", "") };
                pageModel.CorreoParaCrear.Sistemas = new List<SelectListItem> { new SelectListItem("Seleccione Sistema...", "") };
            }
        }



    }
}
