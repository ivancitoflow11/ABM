using ABM.Models;
using ABM.Servicios; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting; 
using System.IO;             
using System.Threading.Tasks;    
using Microsoft.Extensions.Logging; 
using System;                    

namespace ABM.Controllers
{
    [Authorize] 
    public class PerfilController : Controller
    {
        private readonly IRepositorioUsuarios _repositorioUsuarios;
        private readonly IWebHostEnvironment _webHostEnvironment; 
        private readonly IHttpContextAccessor _httpContextAccessor; 
        private readonly ILogger<PerfilController> _logger;

        public PerfilController(
            IRepositorioUsuarios repositorioUsuarios,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PerfilController> logger)
        {
            _repositorioUsuarios = repositorioUsuarios;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Método privado para obtener el ID del usuario actual de forma segura
        private int GetCurrentUserIdInt()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("No se pudo obtener el ID del usuario de los claims.");
            return 0; 
        }


		[Authorize] // Solo usuarios logeados pueden acceder
		[HttpGet]
		public IActionResult CambiarPasswordUsuarioLogeado()
		{
			return View(new CambiarPasswordLogeadoViewModel());
		}

		[Authorize]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CambiarPasswordUsuarioLogeado(CambiarPasswordLogeadoViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			var userIdString = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int idUsuario))
			{
				_logger.LogWarning("No se pudo obtener el ID del usuario para cambiar contraseña.");
				TempData["ErrorMessage"] = "Error al identificar al usuario. Por favor, inicie sesión nuevamente.";
				return RedirectToAction("Login"); // O a la página de AjustesPerfil con error
			}

			var usuarioBd = await _repositorioUsuarios.ObtenerPorId(idUsuario);
			if (usuarioBd == null)
			{
				_logger.LogError($"Usuario con ID {idUsuario} no encontrado al intentar cambiar contraseña.");
				TempData["ErrorMessage"] = "Usuario no encontrado.";
				return RedirectToAction("AjustesPerfil", "Perfil"); // Redirigir a la página de ajustes
			}


			if (usuarioBd.repeat_password != viewModel.PasswordActual)
			{


				ModelState.AddModelError("PasswordActual", "La contraseña actual ingresada es incorrecta.");
				return View(viewModel);
			}


			string nuevaPasswordHasheada = BCrypt.Net.BCrypt.HashPassword(viewModel.NuevaPassword); // EJEMPLO con BCrypt.Net
			string nuevaPasswordPlain = viewModel.NuevaPassword; // Para el campo repeat_password


			// Llama al método del repositorio que crearemos en el siguiente paso
			bool actualizacionExitosa = await _repositorioUsuarios.ActualizarPasswordUsuarioLogeado(
				idUsuario,
				nuevaPasswordHasheada,
				nuevaPasswordPlain // Para repeat_password
			);

			if (actualizacionExitosa)
			{
				_logger.LogInformation($"Contraseña actualizada exitosamente para el usuario ID: {idUsuario}.");
				TempData["SuccessMessage"] = "Tu contraseña ha sido cambiada exitosamente.";
				// Redirigir a la página de Ajustes de Perfil o a donde consideres apropiado
				return RedirectToAction("AjustesPerfil", "Perfil");
			}
			else
			{
				_logger.LogError($"Error al actualizar la contraseña en la BD para el usuario ID: {idUsuario}.");
				ModelState.AddModelError(string.Empty, "Ocurrió un error al intentar cambiar tu contraseña. Por favor, inténtalo de nuevo.");
				return View(viewModel);
			}
		}

		[HttpGet]
        public async Task<IActionResult> AjustesPerfil()
        {
            var usuario = await _repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();

            if (usuario == null)
            {
                // Esto podría pasar si la sesión expiró o el usuario fue eliminado
                _logger.LogWarning($"No se pudieron cargar los datos del perfil para el usuario ID: {GetCurrentUserIdInt()} (o ID no encontrado). Redirigiendo a Login.");
                TempData["ErrorMessage"] = "No se pudieron cargar los datos del perfil. Por favor, inicie sesión nuevamente.";
                // Considera redirigir a una acción de Logout o directamente a Login
                return RedirectToAction("Login", "Acceso"); // Ajusta "Acceso" si tu controlador de login es diferente
            }
            return View(usuario);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustesPerfil(Usuario usuarioDesdeVista, IFormFile fotoPerfilFile)
        {
            int currentUserId = GetCurrentUserIdInt();

            // Verificación de seguridad básica: el ID del usuario en el modelo debe coincidir con el usuario logeado.
            // y el usuario debe estar efectivamente logeado (currentUserId != 0)
            if (currentUserId == 0 || usuarioDesdeVista.idUsuario != currentUserId)
            {
                _logger.LogWarning($"Intento no autorizado de modificar perfil. Usuario logeado ID: {currentUserId}, ID en modelo: {usuarioDesdeVista.idUsuario}");
                TempData["ErrorMessage"] = "Operación no autorizada.";
                return RedirectToAction("Index", "Home"); // O a una página de error/acceso denegado
            }

            // Es crucial obtener la entidad actual de la base de datos para actualizarla.
            // Esto evita que se sobrescriban campos no editables o se pierdan datos si el binding del modelo es parcial.
            var usuarioBd = await _repositorioUsuarios.ObtenerPorId(currentUserId);
            if (usuarioBd == null)
            {
                _logger.LogError($"Error crítico: Usuario con ID {currentUserId} no encontrado en la BD al intentar actualizar perfil.");
                TempData["ErrorMessage"] = "Usuario no encontrado. No se pudo actualizar el perfil.";
                return RedirectToAction("AjustesPerfil");
            }

            // Mapear los campos no editables desde la BD al modelo que vino de la vista
            // para asegurar que no se pierdan y para que ModelState.IsValid funcione correctamente
            // si esos campos tienen validaciones (aunque aquí son principalmente para display).
            usuarioDesdeVista.correo = usuarioBd.correo;
            usuarioDesdeVista.usuario = usuarioBd.usuario;
            usuarioDesdeVista.rut = usuarioBd.rut;
            usuarioDesdeVista.idRol = usuarioBd.idRol;
            // ... otros campos importantes que no se editan en este formulario

            if (ModelState.IsValid) // Valida el modelo `usuarioDesdeVista` con los datos del formulario
            {
                string nombreArchivoUnicoParaGuardar = usuarioBd.FotoUrl; // Por defecto, mantener la foto existente

                if (fotoPerfilFile != null && fotoPerfilFile.Length > 0)
                {
                    // 1. Validación del archivo
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" }; // GIF no está en tu ejemplo de vista
                    var extension = Path.GetExtension(fotoPerfilFile.FileName).ToLowerInvariant();
                    long maxFileSize = 5 * 1024 * 1024; // 5MB (5 * 1024 KB * 1024 Bytes)

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("fotoPerfilFile", "Tipo de archivo no permitido para la foto. Solo JPG, JPEG, PNG.");
                    }
                    else if (fotoPerfilFile.Length > maxFileSize)
                    {
                        ModelState.AddModelError("fotoPerfilFile", "La foto de perfil excede el tamaño máximo de 5MB.");
                    }
                    else
                    {
                        // 2. Eliminar foto antigua (opcional pero recomendado si no es la por defecto)
                        if (!string.IsNullOrEmpty(usuarioBd.FotoUrl) && usuarioBd.FotoUrl != "avatar_default.png") // Asume que "avatar_default.png" es tu imagen por defecto
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "perfil", usuarioBd.FotoUrl);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                    _logger.LogInformation($"Foto de perfil antigua eliminada: {oldImagePath}");
                                }
                                catch (IOException ex)
                                {
                                    _logger.LogError(ex, $"Error al intentar eliminar la foto de perfil antigua: {oldImagePath}");
                                    // Considerar si este error debe detener el proceso o solo loggearse.
                                }
                            }
                        }

                        // 3. Guardar nueva foto
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "perfil");
                        if (!Directory.Exists(uploadsFolder)) // Crear la carpeta si no existe
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        // Generar un nombre de archivo único para evitar colisiones y problemas con cachés
                        nombreArchivoUnicoParaGuardar = Guid.NewGuid().ToString() + extension;
                        string filePath = Path.Combine(uploadsFolder, nombreArchivoUnicoParaGuardar);

                        try
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await fotoPerfilFile.CopyToAsync(fileStream);
                            }
                            _logger.LogInformation($"Nueva foto de perfil guardada: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error al guardar la nueva foto de perfil: {filePath}");
                            ModelState.AddModelError("fotoPerfilFile", "Ocurrió un error al subir la foto. Inténtelo de nuevo.");
                            // No continuar con la actualización de la BD si la foto falló pero era la intención cambiarla.
                        }
                    }
                }

                // Si hubo errores en la validación del archivo, retornar a la vista
                if (!ModelState.IsValid)
                {
                    // Pasar el modelo original de la BD para no perder datos no editables
                    // y mantener la foto actual si la nueva carga falló.
                    // Sin embargo, los datos que el usuario intentó cambiar se perderían si pasamos usuarioBd directamente.
                    // Es mejor pasar usuarioDesdeVista, pero asegurando que FotoUrl refleje la realidad.
                    if (string.IsNullOrEmpty(usuarioDesdeVista.FotoUrl)) // Si no se actualizó por error de archivo
                    {
                        usuarioDesdeVista.FotoUrl = usuarioBd.FotoUrl; // Mantener la foto original
                    }
                    return View(usuarioDesdeVista);
                }

                // Actualizar la entidad de la base de datos con los nuevos valores
                usuarioBd.nombre = usuarioDesdeVista.nombre;
                usuarioBd.apellidos = usuarioDesdeVista.apellidos;
                usuarioBd.telefono = usuarioDesdeVista.telefono;
                usuarioBd.FechaNacimiento = usuarioDesdeVista.FechaNacimiento; // Asegúrate que este campo exista en tu tabla y modelo
                usuarioBd.FotoUrl = nombreArchivoUnicoParaGuardar; // Actualizar con el nuevo nombre de archivo (o el original si no se cambió)

                bool actualizacionExitosa = await _repositorioUsuarios.ActualizarDatosPerfil(usuarioBd);

                if (actualizacionExitosa)
                {
                    _logger.LogInformation($"Perfil actualizado correctamente para el usuario ID: {currentUserId}.");
                    TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
                }
                else
                {
                    _logger.LogError($"No se pudo actualizar el perfil en la BD para el usuario ID: {currentUserId}.");
                    TempData["ErrorMessage"] = "No se pudo actualizar el perfil. Inténtelo de nuevo.";
                }
                return RedirectToAction("AjustesPerfil"); // Redirigir para evitar reenvío del formulario
            }

            // Si ModelState no es válido (por validaciones del modelo Usuario, no del archivo)
            // Devolver el modelo que vino de la vista para que se muestren los errores de validación.
            // Asegurarse que la FotoUrl correcta se muestre si la validación falló por otro campo.
            if (string.IsNullOrEmpty(usuarioDesdeVista.FotoUrl) && !string.IsNullOrEmpty(usuarioBd.FotoUrl))
            {
                usuarioDesdeVista.FotoUrl = usuarioBd.FotoUrl;
            }
            _logger.LogWarning($"ModelState inválido al intentar actualizar perfil para usuario ID: {currentUserId}. Errores: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return View(usuarioDesdeVista);
        }
    }
}