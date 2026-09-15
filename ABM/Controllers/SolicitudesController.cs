using ABM.Filters;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    // "Tiketera" interna de ABM: cualquier usuario logueado puede crear solicitudes
    // y ver las suyas (Index). Sólo los roles con el permiso de menú "Administrar
    // Solicitudes" (ftc_MENU/ftc_PermisosMenu) pueden ver todas y cambiarles el estado (Admin).
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly IRepositorioSolicitudes _repositorioSolicitudes;
        private readonly IRepositorioUsuarios _repositorioUsuarios;

        private static readonly string[] EstadosValidos = { "Pendiente", "En curso", "Completada" };

        public SolicitudesController(IRepositorioSolicitudes repositorioSolicitudes, IRepositorioUsuarios repositorioUsuarios)
        {
            _repositorioSolicitudes = repositorioSolicitudes;
            _repositorioUsuarios = repositorioUsuarios;
        }

        // Mis Solicitudes: formulario de creación + listado de las propias.
        [HttpGet]
        [Monitoreo("Solicitudes", "SELECT", "verMisSolicitudes")]
        public async Task<IActionResult> Index()
        {
            var usuario = await _repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            if (usuario == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            var solicitudes = await _repositorioSolicitudes.ObtenerSolicitudesPorUsuario(usuario.idUsuario);
            return View(solicitudes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("Solicitudes", "INSERT", "crearSolicitud")]
        public async Task<IActionResult> Crear(SolicitudCrearViewModel model)
        {
            var usuario = await _repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            if (usuario == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Debe completar el asunto y el mensaje.";
                return RedirectToAction("Index");
            }

            var creada = await _repositorioSolicitudes.CrearSolicitud(usuario.idUsuario, model.Asunto, model.Mensaje);

            TempData[creada ? "SuccessMessage" : "ErrorMessage"] = creada
                ? "Tu solicitud fue enviada correctamente."
                : "No se pudo crear la solicitud. Intenta nuevamente.";

            return RedirectToAction("Index");
        }

        // Administrar Solicitudes: todas las solicitudes de todos los usuarios.
        [HttpGet]
        [Monitoreo("Solicitudes", "SELECT", "verAdministrarSolicitudes")]
        public async Task<IActionResult> Admin()
        {
            var usuario = await _repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            if (usuario == null || !usuario.idRol.HasValue)
            {
                return RedirectToAction("Login", "Acceso");
            }

            var tienePermiso = await _repositorioSolicitudes.TienePermisoAdministrarSolicitudes(usuario.idRol.Value);
            if (!tienePermiso)
            {
                TempData["ErrorMessage"] = "No tiene permisos para administrar solicitudes.";
                return RedirectToAction("Index", "Home");
            }

            var solicitudes = await _repositorioSolicitudes.ObtenerTodasLasSolicitudes();
            return View(solicitudes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Monitoreo("Solicitudes", "UPDATE", "cambiarEstadoSolicitud")]
        public async Task<IActionResult> CambiarEstado(int idSolicitud, string nuevoEstado)
        {
            var usuario = await _repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            if (usuario == null || !usuario.idRol.HasValue)
            {
                return Json(new { success = false, message = "Sesión inválida. Vuelva a iniciar sesión." });
            }

            var tienePermiso = await _repositorioSolicitudes.TienePermisoAdministrarSolicitudes(usuario.idRol.Value);
            if (!tienePermiso)
            {
                return Json(new { success = false, message = "No tiene permisos para realizar esta acción." });
            }

            if (!EstadosValidos.Contains(nuevoEstado))
            {
                return Json(new { success = false, message = "Estado no válido." });
            }

            var actualizado = await _repositorioSolicitudes.CambiarEstado(idSolicitud, nuevoEstado, usuario.idUsuario);

            return Json(new
            {
                success = actualizado,
                message = actualizado ? "Estado actualizado correctamente." : "No se pudo actualizar el estado."
            });
        }
    }
}
