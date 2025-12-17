using System; // Necesario para Exception
using System.Collections.Generic; // Necesario para List<>
using System.Linq;
using System.Threading.Tasks;
using ABM.Filters;
using ABM.Servicios;
using ABM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class ConsultaUsuarioController : Controller
    {
        private readonly IRepositorioConsultaUsuario _repositorio;

        public ConsultaUsuarioController(IRepositorioConsultaUsuario repositorio)
        {
            _repositorio = repositorio;
        }

        [HttpGet]
        [Monitoreo("ConsultaUsuario", "SELECT", "verFormularioBusquedaUsuario")]
        public IActionResult Buscar()
        {
            var vm = new ConsultaUsuarioViewModel();
            return View(vm);
        }

        [HttpPost]
        [Monitoreo("ConsultaUsuario", "SELECT", "buscarDatosUsuario")]
        public async Task<IActionResult> Buscar(ConsultaUsuarioViewModel vm)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(vm.Input))
                {
                    // 1. Intentamos obtener datos básicos
                    vm.Usuario = await _repositorio.ObtenerDatosBasicos(vm.Input);

                    // 2. CORRECCIÓN: Quitamos '&& vm.Input.Contains(" ")'
                    // Si no encontró nada exacto, buscamos coincidencias SIEMPRE.
                    if (vm.Usuario == null)
                    {
                        var coincidencias = await _repositorio.BuscarCoincidencias(vm.Input);
                        vm.Usuario = coincidencias.FirstOrDefault();
                    }

                    if (vm.Usuario != null)
                    {
                        // Usamos MailUsuario y RutDni (Nuevos nombres)
                        var datosAD = await _repositorio.ObtenerDatosAD(vm.Usuario.MailUsuario, vm.Usuario.RutDni);
                        vm.EstadoAD = datosAD != null ? "ACTIVO" : "NO ENCONTRADO";
                        vm.UltimoLoginAD = datosAD?.ultimo_login;

                        var claveFiniq = string.IsNullOrWhiteSpace(vm.Usuario.RutDni) ? vm.Input : vm.Usuario.RutDni;
                        var datosFiniquito = await _repositorio.ObtenerDatosFiniquito(claveFiniq);
                        vm.EsFiniquitado = datosFiniquito != null;
                        vm.FechaFiniquito = datosFiniquito?.fecfiniquito;

                        var claveSpr = !string.IsNullOrWhiteSpace(vm.Usuario.RutDni) ? vm.Usuario.RutDni
                                      : (!string.IsNullOrWhiteSpace(vm.Usuario.MailUsuario) ? vm.Usuario.MailUsuario : vm.Input);

                        var (spr, emp) = await _repositorio.ObtenerEstadoSprEmpCentral(claveSpr);
                        vm.SprActivo = spr;
                        vm.EmpCentralActivo = emp;

                        vm.Sistemas = await _repositorio.ObtenerSistemas(claveSpr);
                    }
                    else
                    {
                        // Opcional: Agregar mensaje de error si no se encuentra nada
                        ModelState.AddModelError("", "No se encontraron resultados para la búsqueda.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log para que veas el error en la consola de Visual Studio
                System.Diagnostics.Debug.WriteLine($"ERROR EN BUSCAR POST: {ex.Message}");
                ModelState.AddModelError("", "Ocurrió un error al buscar. Intente nuevamente.");
            }

            return View(vm);
        }

        [HttpGet]
        [Monitoreo("ConsultaUsuario", "SELECT", "autocompleteUsuarios")]
        public async Task<IActionResult> Autocomplete(string term)
        {
            try
            {
                var resultados = await _repositorio.BuscarCoincidencias(term ?? "");
                return Json(resultados);
            }
            catch (Exception ex)
            {
                // Esto evita que el JS explote con "<!DOCTYPE..."
                System.Diagnostics.Debug.WriteLine($"ERROR AUTOCOMPLETE: {ex.Message}");
                // Devuelve una lista vacía en formato JSON válido
                return Json(new List<object>());
            }
        }
    }
}