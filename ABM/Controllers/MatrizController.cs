using System.Security.Claims;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // <-- AÑADIR ESTE USING

namespace ABM.Controllers
{
    public class MatrizController : Controller
    {
        private readonly IRepositorioMatriz _repositorioMatriz;
        private readonly ILogger<MatrizController> _logger; // <-- AÑADIR CAMPO PARA EL LOGGER

        // CAMBIO: Inyectar ILogger en el constructor
        public MatrizController(IRepositorioMatriz repositorioMatriz, ILogger<MatrizController> logger)
        {
            _repositorioMatriz = repositorioMatriz;
            _logger = logger; // <-- ASIGNAR EL LOGGER
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Leer el contexto de la Sesión
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            // 2. Validar que el contexto exista
            if (idPaisSesion == null || idNegocioSesion == null)
            {
                // Redirigir a tu selector de contexto o mostrar vista vacía
                return View(new List<MatrizDisponible>());
            }

            // 3. Llamar al repositorio con los datos de la sesión
            var matricesDisponibles = await _repositorioMatriz.ObtenerMatricesDisponibles(
                idPaisSesion.Value,
                idNegocioSesion.Value
            );

            return View(matricesDisponibles);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int idSistema, string idPais)
        {
            // 1. Validar los parámetros de la ruta
            if (idSistema <= 0 || string.IsNullOrEmpty(idPais))
            {
                return BadRequest("Los parámetros proporcionados no son válidos.");
            }

            // 2. CAMBIO: Leer el IdNegocio desde la sesión
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            // 3. CAMBIO: Validar que el IdNegocio exista en la sesión
            if (idNegocioSesion == null)
            {
                _logger.LogWarning("Se intentó acceder al detalle de la matriz sin un IdNegocio en la sesión.");
                // Redirigir a una página donde pueda seleccionar el contexto si este se pierde.
                return RedirectToAction("Index");
            }

            // 4. CAMBIO: Llamar al repositorio con los tres parámetros (idSistema, idPais, idNegocio)
            var datosMatriz = await _repositorioMatriz.ObtenerListaDataMatrizTodo(idSistema, idPais, idNegocioSesion.Value);

            // 5. CAMBIO: Poblar ViewData con la información completa para el título
            ViewData["Pais"] = idPais;
            ViewData["Sistema"] = datosMatriz.FirstOrDefault()?.Sistema ?? "Desconocido";
            ViewData["Negocio"] = datosMatriz.FirstOrDefault()?.Negocio ?? "Desconocido"; // <-- Se añade el Negocio

            return View(datosMatriz);
        }
    }
}
