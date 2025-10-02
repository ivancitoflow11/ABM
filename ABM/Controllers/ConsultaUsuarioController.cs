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
        [HttpPost]
        public async Task<IActionResult> Buscar(ConsultaUsuarioViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.Input))
            {
                vm.Usuario = await _repositorio.ObtenerDatosBasicos(vm.Input);

                if (vm.Usuario == null && vm.Input.Contains(" "))
                {
                    var coincidencias = await _repositorio.BuscarCoincidencias(vm.Input);
                    vm.Usuario = coincidencias.FirstOrDefault();
                }

                if (vm.Usuario != null)
                {
                    vm.EstadoAD = await _repositorio.ExisteEnADPorMailORut(vm.Usuario.Correo, vm.Usuario.Rut)
                                ? "ACTIVO" : "NO ENCONTRADO";

                    var claveFiniq = string.IsNullOrWhiteSpace(vm.Usuario.Rut) ? vm.Input : vm.Usuario.Rut;
                    vm.EsFiniquitado = await _repositorio.EstaFiniquitado(claveFiniq);

                    var claveSpr = !string.IsNullOrWhiteSpace(vm.Usuario.Rut) ? vm.Usuario.Rut
                                  : (!string.IsNullOrWhiteSpace(vm.Usuario.Correo) ? vm.Usuario.Correo : vm.Input);
                    var (spr, emp) = await _repositorio.ObtenerEstadoSprEmpCentral(claveSpr);
                    vm.SprActivo = spr;
                    vm.EmpCentralActivo = emp;

                    // 👇 NUEVO - Obtener sistemas
                    vm.Sistemas = await _repositorio.ObtenerSistemas(claveSpr);
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
