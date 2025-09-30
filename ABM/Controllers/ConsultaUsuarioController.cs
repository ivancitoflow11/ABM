using System.Linq;
using System.Threading.Tasks;
using ABM.Models;
using ABM.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class ConsultaUsuarioController : Controller
    {
        private readonly IRepositorioConsultaUsuario _repo;

        public ConsultaUsuarioController(IRepositorioConsultaUsuario repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ConsultaUsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buscar(string input)
        {
            var vm = new ConsultaUsuarioViewModel { Input = input };

            vm.DatosBasicos = await _repo.ObtenerDatosBasicos(input);
            vm.Estados = (await _repo.ObtenerEstados(input)).ToList();

            vm.TieneProblemas = vm.Estados.Any(e => e.NombreEstado != "FINIQUITADO" && e.Valor != "ACTIVO")
                                || vm.Estados.Any(e => e.NombreEstado == "FINIQUITADO" && e.Valor == "SI");

            vm.Mensaje = vm.DatosBasicos is null && !vm.Estados.Any()
                ? "Sin resultados."
                : (vm.TieneProblemas ? "El usuario presenta problemas." : "El usuario no presenta problemas.");

            return View("Index", vm);
        }
    }
}
