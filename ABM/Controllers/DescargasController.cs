using Microsoft.AspNetCore.Mvc;
using ABM.Filters;
using ABM.Servicios;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Necesario para CookieOptions

namespace ABM.Controllers
{
	[Authorize]
	[AutorizarMenu]
	public class DescargasController : Controller
	{
		private const string RecursoDatosAD = "DatosAD";
		private const string RecursoFiniquitados = "FiniquitadosGeneral";
		private const string RecursoActivosFalanet = "ActivosFalanet";

		private static readonly string[] RecursosPermitidos = { RecursoDatosAD, RecursoFiniquitados, RecursoActivosFalanet };

		private readonly IRepositorioDescargas _repositorioDescargas;
		private readonly IDescargaTokenService _tokenService;

		public DescargasController(IRepositorioDescargas repositorioDescargas, IDescargaTokenService tokenService)
		{
			_repositorioDescargas = repositorioDescargas;
			_tokenService = tokenService;
		}

		private int ObtenerIdUsuarioActual()
		{
			int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario);
			return idUsuario;
		}

		// Emite un token firmado (atado al usuario y al recurso, vigencia corta, un solo uso) que el cliente
		// debe presentar al pedir el CSV. "id" es el identificador que usa el JS para las cookies de progreso.
		[HttpGet]
		[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		public IActionResult EmitirToken(string recurso)
		{
			if (!RecursosPermitidos.Contains(recurso))
			{
				return BadRequest();
			}

			var idUsuario = ObtenerIdUsuarioActual();
			if (idUsuario <= 0)
			{
				return Unauthorized();
			}

			var (token, id) = _tokenService.Emitir(idUsuario, recurso);
			return Json(new { downloadToken = token, id });
		}

		// Método privado refactorizado para generar CSV y establecer cookie
		private IActionResult GenerarRespuestaCsv(IEnumerable<dynamic> datos, string nombreBaseArchivo, string downloadId)
		{
			// Cookie options: HttpOnly = false para que JS pueda leerla (solo necesitamos verificar su existencia)
			// Path = "/" para que sea accesible en toda la aplicación.
			// Expires es para auto-limpieza.
			var cookieOptions = new CookieOptions { Path = "/", HttpOnly = false, Expires = System.DateTime.Now.AddMinutes(5) };

			if (datos == null || !datos.Any())
			{
				TempData["ErrorMessage"] = $"No hay datos para descargar de '{nombreBaseArchivo}'.";
				Response.Cookies.Append($"download_error_{downloadId}", "nodata", cookieOptions);
				return RedirectToAction("Index", "Home"); // O la vista actual
			}

			var sb = new StringBuilder();
			var firstItem = datos.First() as IDictionary<string, object>;

			if (firstItem == null)
			{
				TempData["ErrorMessage"] = $"Error al procesar los datos para CSV de '{nombreBaseArchivo}'.";
				Response.Cookies.Append($"download_error_{downloadId}", "processing", cookieOptions);
				return RedirectToAction("Index", "Home");
			}

			var headers = firstItem.Keys;
			sb.AppendLine(string.Join(",", headers.Select(header => QuoteValue(header))));

			foreach (var dato in datos)
			{
				var item = dato as IDictionary<string, object>;
				var values = new List<string>();
				foreach (var header in headers)
				{
					var value = item[header]?.ToString() ?? "";
					values.Add(QuoteValue(value));
				}
				sb.AppendLine(string.Join(",", values));
			}

			// Establecer cookie de éxito ANTES de devolver el archivo
			Response.Cookies.Append($"download_status_{downloadId}", "completed", cookieOptions);

			string fechaActual = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string fileName = $"{nombreBaseArchivo}_{fechaActual}.csv";
			return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
		}

		private string QuoteValue(string value)
		{
			if (string.IsNullOrEmpty(value)) return "";
			if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
			{
				return $"\"{value.Replace("\"", "\"\"")}\"";
			}
			return value;
		}

        // Acción para Datos AD (NUEVO)
        [HttpGet]
        [Monitoreo("DescargarDatosADCSV", "SELECT", "descargarDatosADCSV")]
        public async Task<IActionResult> DescargarDatosADCSV(string downloadToken)
        {
            if (!_tokenService.TryConsumir(downloadToken, ObtenerIdUsuarioActual(), RecursoDatosAD, out var downloadId))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var datos = await _repositorioDescargas.ObtenerDatosAD(ObtenerIdUsuarioActual());
            return GenerarRespuestaCsv(datos, "DatosAD", downloadId);
        }

        // Acción para Finiquitados
        [HttpGet]
        [Monitoreo("DescargarFiniquitadosCSV", "SELECT", "descargarFiniquitadosCSV")]
        public async Task<IActionResult> DescargarFiniquitadosCSV(string downloadToken)
		{
			if (!_tokenService.TryConsumir(downloadToken, ObtenerIdUsuarioActual(), RecursoFiniquitados, out var downloadId))
			{
				return StatusCode(StatusCodes.Status403Forbidden);
			}

			var datos = await _repositorioDescargas.ObtenerDatosFiniquitados();
			return GenerarRespuestaCsv(datos, "FiniquitadosGeneral", downloadId);
		}

		// Acción para Activos Falanet
		[HttpGet]
        [Monitoreo("DescargarActivosFalanetCSV", "SELECT", "descargarActivosFalanetCSV")]
        public async Task<IActionResult> DescargarActivosFalanetCSV(string downloadToken)
		{
			if (!_tokenService.TryConsumir(downloadToken, ObtenerIdUsuarioActual(), RecursoActivosFalanet, out var downloadId))
			{
				return StatusCode(StatusCodes.Status403Forbidden);
			}

			var datos = await _repositorioDescargas.ObtenerDatosActivosFalanet();
			return GenerarRespuestaCsv(datos, "ActivosFalanet", downloadId);
		}
	}
}
