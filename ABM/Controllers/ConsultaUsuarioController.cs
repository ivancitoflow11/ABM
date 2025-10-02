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
                    // AD (ya lo tenías)
                    vm.EstadoAD = await _repositorio.ExisteEnADPorMailORut(vm.Usuario.Correo, vm.Usuario.Rut)
                                ? "ACTIVO" : "NO ENCONTRADO";

                    // Finiquitado (ya lo tenías)
                    var claveFiniq = string.IsNullOrWhiteSpace(vm.Usuario.Rut) ? vm.Input : vm.Usuario.Rut;
                    vm.EsFiniquitado = await _repositorio.EstaFiniquitado(claveFiniq);

                    // 👇 SPR / Emp. Central
                    var claveSpr = !string.IsNullOrWhiteSpace(vm.Usuario.Rut) ? vm.Usuario.Rut
                                  : (!string.IsNullOrWhiteSpace(vm.Usuario.Correo) ? vm.Usuario.Correo : vm.Input);

                    var (spr, emp) = await _repositorio.ObtenerEstadoSprEmpCentral(claveSpr);
                    vm.SprActivo = spr;
                    vm.EmpCentralActivo = emp;
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
