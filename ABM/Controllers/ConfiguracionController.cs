using ABM.Models;
using ABM.Filters;
using ABM.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABM.Controllers
{
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
    }
}
