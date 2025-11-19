
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ABM.Filters;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABM.Controllers
{
    [Authorize]
    public class CargarArchivosADController : Controller
    {
        private readonly IRepositorioCargarArchivosAD _repo;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public CargarArchivosADController(IRepositorioCargarArchivosAD repo, IWebHostEnvironment webHostEnvironment)
        {
            _repo = repo;
            _webHostEnvironment = webHostEnvironment;
        }


        [HttpGet]
        [Monitoreo("CargarArchivosAD", "SELECT", "verPaginaCargarArchivosAD")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new CargarArchivosViewModel();

            var paises = await _repo.ObtenerPaisesUnicosAsync();
            viewModel.Paises = new SelectList(paises);

            viewModel.HistoricoCargas = await _repo.ObtenerHistoricoCargasAsync();

            return View(viewModel);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(524288000)]
        [Monitoreo("CargarArchivosAD", "INSERT", "cargarArchivoAD")]
        public async Task<IActionResult> Cargar(CargarArchivosViewModel viewModel, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("archivo", "Debe seleccionar un archivo para cargar.");
            }

            var idCruce = await _repo.ObtenerIdCruceKpiAsync(viewModel.PaisSeleccionado, viewModel.NegocioSeleccionado);
            if (!idCruce.HasValue)
            {
                ModelState.AddModelError("", "La combinación de País y Negocio seleccionada no es válida o no existe.");
            }

            if (!ModelState.IsValid)
            {
                var paises = await _repo.ObtenerPaisesUnicosAsync();
                viewModel.Paises = new SelectList(paises, viewModel.PaisSeleccionado);
                if (!string.IsNullOrEmpty(viewModel.PaisSeleccionado))
                {
                    var negocios = await _repo.ObtenerNegociosPorPaisAsync(viewModel.PaisSeleccionado);
                    viewModel.Negocios = new SelectList(negocios, viewModel.NegocioSeleccionado);
                }
                viewModel.HistoricoCargas = await _repo.ObtenerHistoricoCargasAsync();
                TempData["ErrorMessage"] = "Hubo un error al procesar la solicitud. Por favor, revise los campos.";
                return View("Index", viewModel);
            }


            string nombreFinalArchivo = null;
            if (archivo != null && archivo.Length > 0)
            {
                string carpetaArchivos = Path.Combine(_webHostEnvironment.WebRootPath, "archivos");
                if (!Directory.Exists(carpetaArchivos))
                {
                    Directory.CreateDirectory(carpetaArchivos);
                }


                string nombreBase = Path.GetFileNameWithoutExtension(archivo.FileName);
                string extension = Path.GetExtension(archivo.FileName);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");


                nombreFinalArchivo = $"{nombreBase}_{timestamp}{extension}";

                string rutaArchivo = Path.Combine(carpetaArchivos, nombreFinalArchivo);

                using (var fileStream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await archivo.CopyToAsync(fileStream);
                }
            }


            var historico = new HistoricoCargaKPI
            {
                IdCrucesKPI = idCruce.Value,
                Fecha = viewModel.FechaCarga,
                Archivo = nombreFinalArchivo 
            };

            await _repo.CrearHistoricoCargaAsync(historico);

            TempData["MensajeExito"] = "Archivo cargado y registro guardado exitosamente.";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Monitoreo("NegociosPorPais", "SELECT", "obtenerNegociosPorPais")]
        public async Task<JsonResult> GetNegociosPorPais(string pais)
        {
            if (string.IsNullOrEmpty(pais))
            {
                return Json(new SelectList(Enumerable.Empty<SelectListItem>()));
            }
            var negocios = await _repo.ObtenerNegociosPorPaisAsync(pais);
            return Json(new SelectList(negocios));
        }


        [HttpGet]
        [Monitoreo("CargarArchivosAD", "SELECT", "descargarArchivoAD")]
        public IActionResult DescargarArchivo(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return NotFound();
            }

            string carpetaArchivos = Path.Combine(_webHostEnvironment.WebRootPath, "archivos");
            string rutaArchivo = Path.Combine(carpetaArchivos, nombreArchivo);

            if (!System.IO.File.Exists(rutaArchivo))
            {

                return NotFound();
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(rutaArchivo);
            return File(fileBytes, "application/octet-stream", nombreArchivo); 
        }
    }
}