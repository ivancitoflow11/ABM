using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABM.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly IRepositorioCargas repositorioCargas;
        private readonly IRepositorioReportes repositorioReportes;
        public ReportesController(IRepositorioCargas repositorioCargas,
            IRepositorioReportes repositorioReportes)
        {
            this.repositorioCargas = repositorioCargas;
            this.repositorioReportes = repositorioReportes;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ReporteAD()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Finiquitados(string sistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(sistema))
            {
                model.ListaFiltroFiniquitados = await repositorioReportes.ObtenerListaFiniquitadosPorSistema(sistema, idUsuarioLogueado);
                model.sistema = sistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Finiquitados(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Actualizar la lista de sistemas en el modelo
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(model.sistema))
            {
                model.ListaFiltroFiniquitados = await repositorioReportes.ObtenerListaFiniquitadosPorSistema(model.sistema, idUsuarioLogueado);
            }

            return View("Finiquitados", model);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UsuariosNoEncontrados(string sistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(sistema))
            {
                model.ListaUsuariosNoEncontrados = await repositorioReportes.ObtenerListaUsuariosNoEncontradosPorSistema(sistema, idUsuarioLogueado);
                model.sistema = sistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UsuariosNoEncontrados(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Actualizar la lista de sistemas en el modelo
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(model.sistema))
            {
                model.ListaUsuariosNoEncontrados = await repositorioReportes.ObtenerListaUsuariosNoEncontradosPorSistema(model.sistema, idUsuarioLogueado);
            }

            return View("UsuariosNoEncontrados", model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BuscarUsuarios(string rutdni, string nombreusuario)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();

            // Si se proporciona rut o nombre, buscar usuarios
            if (!string.IsNullOrEmpty(rutdni) || !string.IsNullOrEmpty(nombreusuario))
            {
                model.ListaBuscarUsuario = await repositorioReportes.ObtenerUserPorNombreORut(nombreusuario, rutdni, idUsuarioLogueado);
                model.nombreusuario = nombreusuario;
                model.rutdni = rutdni;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BuscarUsuarios(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Si se proporciona rut o nombre, buscar usuarios
            if (!string.IsNullOrEmpty(model.rutdni) || !string.IsNullOrEmpty(model.nombreusuario))
            {
                model.ListaBuscarUsuario = await repositorioReportes.ObtenerUserPorNombreORut(model.nombreusuario, model.rutdni, idUsuarioLogueado);
            }

            return View("BuscarUsuarios", model);
        }


        public IActionResult GestionAD()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UltimaConexion(string Sistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            ReportesViewModel model = new ReportesViewModel();

            // Obtener la lista de sistemas
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(Sistema))
            {
                model.ListaFiltroUltimaConexion = await repositorioReportes.ObtenerListaUltimaConexion(Sistema, idUsuarioLogueado);
                model.sistema = Sistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UltimaConexion(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Obtener la lista de sistemas
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(model.sistema))
            {
                model.ListaFiltroUltimaConexion = await repositorioReportes.ObtenerListaUltimaConexion(model.sistema, idUsuarioLogueado);
            }

            return View("UltimaConexion", model);
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> InactividadUsuarios(string sistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(sistema))
            {
                model.ListaFiltroInactividad = await repositorioReportes.ObtenerListaInactividadPorSistema(sistema, idUsuarioLogueado);
                model.sistema = sistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> InactividadUsuarios(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Actualizar la lista de sistemas en el modelo
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (!string.IsNullOrEmpty(model.sistema))
            {
                model.ListaFiltroInactividad = await repositorioReportes.ObtenerListaInactividadPorSistema(model.sistema, idUsuarioLogueado);
            }

            return View("InactividadUsuarios", model);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DifCargoPerfil(int? idSistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();
            model.ListaSistemas = await repositorioCargas.SistemaCargoPerfil();

            // Si se proporciona un sistema, filtrar los datos
            if (idSistema != null)
            {
                model.ListaDifCargoPerfil = await repositorioReportes.ObtenerListaDifCargoPerfilPorSistema(idSistema, idUsuarioLogueado);
                model.idSistema = idSistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DifCargoPerfil(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Actualizar la lista de sistemas en el modelo
            model.ListaSistemas = await repositorioCargas.SistemaCargoPerfil();

            // Si se proporciona un sistema, filtrar los datos
            if (model.idSistema != null)
            {
                model.ListaDifCargoPerfil = await repositorioReportes.ObtenerListaDifCargoPerfilPorSistema(model.idSistema, idUsuarioLogueado);
            }

            return View("DifCargoPerfil", model);
        }



        public IActionResult ExcepcionesAutorizadas()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UsuariosActivos(int? idSistema)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (idSistema != null)
            {
                model.ListaUsuariosActivos = await repositorioReportes.ObtenerListaUsuariosActivosPorSistema(idSistema, idUsuarioLogueado);
                model.idSistema = idSistema;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UsuariosActivos(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Actualizar la lista de sistemas en el modelo
            model.ListaSistemas = await repositorioCargas.ListaDeSistemas();

            // Si se proporciona un sistema, filtrar los datos
            if (model.idSistema != null)
            {
                model.ListaUsuariosActivos = await repositorioReportes.ObtenerListaUsuariosActivosPorSistema(model.idSistema, idUsuarioLogueado);
            }

            return View("UsuariosActivos", model);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AsignacionCargoMatriz(string rutdni)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Inicializar el modelo de vista
            ReportesViewModel model = new ReportesViewModel();

            // Si se proporciona rut, obtener la información del usuario
            if (!string.IsNullOrEmpty(rutdni))
            {
                model.ListaBuscarUsuario = await repositorioReportes.ObtenerUserPorRut(rutdni, idUsuarioLogueado);
                model.ListaInfoUsuario = await repositorioReportes.ObtenerCargoPorRut(rutdni, idUsuarioLogueado);
                model.rutdni = rutdni;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AsignacionCargoMatriz(ReportesViewModel model)
        {
            // Obtener el ID del usuario logueado
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioString, out int idUsuarioLogueado))
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }

            // Si se proporciona rut, obtener la información del usuario
            if (!string.IsNullOrEmpty(model.rutdni))
            {
                model.ListaBuscarUsuario = await repositorioReportes.ObtenerUserPorRut(model.rutdni, idUsuarioLogueado);
                model.ListaInfoUsuario = await repositorioReportes.ObtenerCargoPorRut(model.rutdni, idUsuarioLogueado);
            }

            return View("AsignacionCargoMatriz", model);
        }

    }
}
