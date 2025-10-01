using Microsoft.AspNetCore.Mvc;
using ABM.ViewModels;
using ABM.Servicios;
using System.Threading.Tasks;

namespace ABM.Controllers
{
    public class ConsultaUsuarioController : Controller
    {
        private readonly IRepositorioConsultaUsuario _repositorio;

        public ConsultaUsuarioController(IRepositorioConsultaUsuario repositorio)
        {
            _repositorio = repositorio;
        }

        [HttpGet]
        public IActionResult Buscar()
        {
            var vm = new ConsultaUsuarioViewModel();
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Buscar(ConsultaUsuarioViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.Input))
            {
                vm.Usuario = await _repositorio.ObtenerDatosBasicos(vm.Input);

                if (vm.Usuario != null)
                {
                    // usa Correo y/o Rut para lookup exacto en AD
                    vm.EstadoAD = await _repositorio.ExisteEnADPorMailORut(vm.Usuario.Correo, vm.Usuario.Rut)
                                ? "ACTIVO"
                                : "NO ENCONTRADO";
                }
            }

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> Autocomplete(string term)
        {
            var resultados = await _repositorio.BuscarCoincidencias(term ?? "");
            return Json(resultados);
        }

    }
}
