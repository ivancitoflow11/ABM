using System.Linq;
using System.Threading.Tasks;
using ABM.Servicios;
using Dapper; // Necesario para la consulta directa de Dapper
using Microsoft.AspNetCore.Authorization; // Asegúrate de tener la autorización
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Necesario para la conexión directa
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ABM.Controllers
{
    [Authorize] // Es una buena práctica proteger todo el controlador
    public class MatrizController : Controller
    {
        private readonly IRepositorioMatriz _repositorioMatriz;
        private readonly ILogger<MatrizController> _logger;

        public MatrizController(IRepositorioMatriz repositorioMatriz, ILogger<MatrizController> logger)
        {
            _repositorioMatriz = repositorioMatriz;
            _logger = logger;
        }

        private int ObtenerIdUsuarioActual()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (idPaisSesion == null || idNegocioSesion == null)
            {
                // Considera redirigir a una página para seleccionar el contexto
                return View(new List<Models.MatrizDisponible>());
            }

            var matricesDisponibles = await _repositorioMatriz.ObtenerMatricesDisponibles(
                idPaisSesion.Value,
                idNegocioSesion.Value
            );

            return View(matricesDisponibles);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int idSistema, string idPais, int idNegocio)
        {
            if (idSistema <= 0 || string.IsNullOrEmpty(idPais) || idNegocio <= 0)
            {
                return BadRequest("Los parámetros proporcionados no son válidos.");
            }

            var datosMatriz = await _repositorioMatriz.ObtenerListaDataMatrizTodo(idSistema, idPais, idNegocio);

            if (!datosMatriz.Any())
            {
                ViewData["Title"] = "Sin Datos";
                ViewBag.PuedeFirmar = false;
                ViewBag.FirmaExistente = null;
                return View(new List<Models.MatrizActivo>());
            }

            var primerRegistro = datosMatriz.First();
            ViewData["Pais"] = primerRegistro.Pais;
            ViewData["Negocio"] = primerRegistro.Negocio;
            ViewData["Sistema"] = primerRegistro.Sistema;

            // --- INICIA LÓGICA DE FIRMA CORREGIDA ---
            var idUsuario = ObtenerIdUsuarioActual();
            if (idUsuario > 0)
            {
                // Inicializamos los ViewBag para evitar errores si algo falla.
                ViewBag.PuedeFirmar = false;
                ViewBag.FirmaExistente = null;

                var esResponsable = await _repositorioMatriz.EsResponsableFirma(idUsuario);

                int idPaisDb;
                using (var connection = new SqlConnection(_repositorioMatriz.GetConnectionString()))
                {
                    // Hacemos la búsqueda más segura, ignorando espacios y mayúsculas/minúsculas.
                    idPaisDb = await connection.QuerySingleOrDefaultAsync<int>(
                        "SELECT idPais FROM ftc_pais WHERE UPPER(TRIM(pais)) = UPPER(TRIM(@nombrePais))",
                        new { nombrePais = idPais });
                }

                // Solo continuamos si encontramos el ID del país y el usuario es responsable.
                if (idPaisDb > 0 && esResponsable)
                {
                    var firmaExistente = await _repositorioMatriz.ObtenerFirmaExistente(idUsuario, idPaisDb, idNegocio, idSistema);
                    ViewBag.FirmaExistente = firmaExistente;
                    // La condición final: puede firmar si es responsable Y no hay firma existente.
                    ViewBag.PuedeFirmar = esResponsable && firmaExistente == null;
                }

                // Siempre pasamos los IDs a la vista para los campos ocultos.
                ViewBag.IdSistema = idSistema;
                ViewBag.IdPais = idPaisDb;
                ViewBag.IdNegocio = idNegocio;
            }
            else
            {
                ViewBag.PuedeFirmar = false;
                ViewBag.FirmaExistente = null;
            }
            // --- TERMINA LÓGICA DE FIRMA ---

            return View(datosMatriz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Buena práctica para seguridad
        public async Task<IActionResult> FirmarMatriz(int idPais, int idNegocio, int idSistema, string comentario)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (idUsuario == 0)
            {
                return Json(new { success = false, message = "Error de autenticación. Su sesión puede haber expirado." });
            }

            var esResponsable = await _repositorioMatriz.EsResponsableFirma(idUsuario);
            if (!esResponsable)
            {
                return Json(new { success = false, message = "No tiene permisos para realizar esta acción." });
            }

            var resultado = await _repositorioMatriz.CrearFirmaYDetallesAsync(idUsuario, idPais, idNegocio, idSistema, comentario);

            if (resultado)
            {
                return Json(new { success = true, message = "Matriz firmada exitosamente." });
            }

            return Json(new { success = false, message = "No se pudo crear la firma. Es posible que ya haya sido firmada este mes." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarFirma(int idFirma)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (idUsuario == 0)
            {
                return Json(new { success = false, message = "Error de autenticación." });
            }

            // Aquí podrías añadir validaciones extra, ej: solo el que firmó o un admin puede borrar.

            var resultado = await _repositorioMatriz.EliminarFirmaAsync(idFirma);
            if (resultado)
            {
                return Json(new { success = true, message = "Firma eliminada correctamente." });
            }

            return Json(new { success = false, message = "No se pudo eliminar la firma." });
        }
    }
}