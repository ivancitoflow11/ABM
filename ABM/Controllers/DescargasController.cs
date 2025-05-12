using Microsoft.AspNetCore.Mvc;
using ABM.Filters;
using ABM.Servicios;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Necesario para CookieOptions

namespace ABM.Controllers
{
	[Authorize]
	public class DescargasController : Controller
	{
		private readonly IRepositorioDescargas _repositorioDescargas;

		public DescargasController(IRepositorioDescargas repositorioDescargas)
		{
			_repositorioDescargas = repositorioDescargas;
		}

		// Método privado refactorizado para generar CSV y establecer cookie
		private IActionResult GenerarRespuestaCsv(IEnumerable<dynamic> datos, string nombreBaseArchivo, string downloadToken)
		{
			// Cookie options: HttpOnly = false para que JS pueda leerla (solo necesitamos verificar su existencia)
			// Path = "/" para que sea accesible en toda la aplicación.
			// Expires es para auto-limpieza.
			var cookieOptions = new CookieOptions { Path = "/", HttpOnly = false, Expires = System.DateTime.Now.AddMinutes(5) };

			if (datos == null || !datos.Any())
			{
				TempData["ErrorMessage"] = $"No hay datos para descargar de '{nombreBaseArchivo}'.";
				if (!string.IsNullOrEmpty(downloadToken))
				{
					// Establecer cookie de error si no hay datos
					Response.Cookies.Append($"download_error_{downloadToken}", "nodata", cookieOptions);
				}
				return RedirectToAction("Index", "Home"); // O la vista actual
			}

			var sb = new StringBuilder();
			var firstItem = datos.First() as IDictionary<string, object>;

			if (firstItem == null)
			{
				TempData["ErrorMessage"] = $"Error al procesar los datos para CSV de '{nombreBaseArchivo}'.";
				if (!string.IsNullOrEmpty(downloadToken))
				{
					// Establecer cookie de error si hay problemas con los datos
					Response.Cookies.Append($"download_error_{downloadToken}", "processing", cookieOptions);
				}
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
			if (!string.IsNullOrEmpty(downloadToken))
			{
				Response.Cookies.Append($"download_status_{downloadToken}", "completed", cookieOptions);
			}

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

		// Acción para Finiquitados
		[HttpGet]
        [Monitoreo("DescargarFiniquitadosCSV", "SELECT", "descargarFiniquitadosCSV")]
        public async Task<IActionResult> DescargarFiniquitadosCSV(string downloadToken) // Aceptar token
		{
			var datos = await _repositorioDescargas.ObtenerDatosFiniquitados();
			return GenerarRespuestaCsv(datos, "FiniquitadosGeneral", downloadToken); // Pasar token
		}

		// Acción para Activos Falanet
		[HttpGet]
        [Monitoreo("DescargarActivosFalanetCSV", "SELECT", "descargarActivosFalanetCSV")]
        public async Task<IActionResult> DescargarActivosFalanetCSV(string downloadToken) // Aceptar token
		{
			var datos = await _repositorioDescargas.ObtenerDatosActivosFalanet();
			return GenerarRespuestaCsv(datos, "ActivosFalanet", downloadToken); // Pasar token
		}
	}
}