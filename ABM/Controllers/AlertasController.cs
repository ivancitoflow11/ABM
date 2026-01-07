using ABM.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ABM.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ABM.Servicios;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ABM.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABM.Controllers
{
    [Authorize]
    public class AlertasController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioUsuarios repositorioUsuarios;
        private readonly IMapper mapper;
        private readonly IRepositorioRoles repositorioRoles;
        private readonly IRepositorioAlertas repositorioAlertas;

        public AlertasController(ILogger<HomeController> logger, IRepositorioUsuarios RepositorioUsuarios, IMapper Mapper, IRepositorioRoles RepositorioRoles, IRepositorioAlertas RepositorioAlertas)
        {
            _logger = logger;
            repositorioUsuarios = RepositorioUsuarios;
            mapper = Mapper;
            repositorioRoles = RepositorioRoles;
            repositorioAlertas = RepositorioAlertas;
        }

        [HttpGet]
        [Monitoreo("AlertaSistema", "SELECT", "verAlertaSistema")]
        public async Task<IActionResult> AlertaSistema(int? idNegocio, int? idSistema, bool buscar = false)
        {
            var modelo = new AlertaSistemaViewModel();

            // 1. Obtener usuario y rol para cargar los negocios permitidos
            var usuario = await repositorioUsuarios.ObtenerDatosUsuarioPerfilLogeado();
            var rolConPNS = (await repositorioRoles.ObtenerRolesConPNS())
                                     .FirstOrDefault(r => r.idRol == usuario.idRol);

            List<PaisNegocioViewModel> listaNegocios = new();
            if (rolConPNS?.PaisesNegocios != null)
            {
                listaNegocios = rolConPNS.PaisesNegocios
                    .GroupBy(x => x.idNegocio)
                    .Select(g => g.First())
                    .ToList();
            }
            ViewBag.Negocios = listaNegocios;

            // 2. Cargar sistemas disponibles para el dropdown (y para usarlos de referencia en el relleno)
            var sistemasDisponibles = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);
            ViewBag.Sistemas = sistemasDisponibles;

            ViewBag.IdNegocioSeleccionado = idNegocio;
            ViewBag.IdSistemaSeleccionado = idSistema;

            if (buscar)
            {
                // 3. Traer los datos REALES de la base de datos (solo trae lo que tiene incidencias)
                var estadisticasCompletas = await repositorioAlertas.ObtenerEstadisticasUsuarios(idNegocio, idSistema);

                // --- INICIO: LÓGICA PARA RELLENAR SISTEMAS SIN DATOS ---

                // Preparamos la lista final
                var listaFinal = new List<EstadisticasUsuarios>();
                listaFinal.AddRange(estadisticasCompletas);

                // Definimos el universo de sistemas a mostrar.
                // Si el usuario seleccionó un sistema específico, filtramos el universo. Si no, son todos los del negocio.
                IEnumerable<ABM.Models.Sistema> universoSistemas = sistemasDisponibles;
                if (idSistema.HasValue)
                {
                    universoSistemas = universoSistemas.Where(s => s.idSistema == idSistema.Value);
                }

                // Intentamos obtener el nombre del negocio por defecto para rellenar la celda vacía
                string nombreNegocioDefecto = " - ";
                string verticalDefecto = " - ";
                if (idNegocio.HasValue && listaNegocios != null)
                {
                    var negocioInfo = listaNegocios.FirstOrDefault(n => n.idNegocio == idNegocio.Value);
                    if (negocioInfo != null) nombreNegocioDefecto = negocioInfo.negocio;
                }

                // Iteramos sobre el universo de sistemas
                foreach (var sys in universoSistemas)
                {
                    // Verificamos si este sistema ya vino en los resultados de la BD
                    bool existe = estadisticasCompletas.Any(e => e.Sistema == sys.sistema);

                    if (!existe)
                    {
                        // ¡FALTA! Agregamos una fila "fantasma" para que salga en el Excel con ceros
                        listaFinal.Add(new EstadisticasUsuarios
                        {
                            Vertical = verticalDefecto,
                            Negocio = nombreNegocioDefecto,
                            Sistema = sys.sistema,
                            Pais = "No hay datos", // Esto saldrá en la columna País en el Excel
                            Finiquitados = 0,
                            NoEncontrados = 0,
                            CtaDuplicadas = 0,
                            Activos = 0,
                            TotalUsuarios = 0,
                            Recontratados = 0,
                            De1a3DiasSinGestion = 0,
                            De4a6DiasSinGestion = 0,
                            MasDe6DiasSinGestion = 0,
                            Bandera = null // Sin bandera
                        });
                    }
                }

                // Asignamos la lista FINAL ordenadita al modelo
                modelo.estadisticas = listaFinal.OrderBy(x => x.Sistema).ToList();

                // --- FIN LÓGICA DE RELLENO ---

                // Carga de detalles (estos no necesitan relleno, si no hay datos, no hay detalles)
                modelo.ListaFiltroFiniquitados = await repositorioAlertas.ObtenerDetalleFiniquitados(idNegocio, idSistema);
                modelo.ListaUsuariosNoEncontrados = await repositorioAlertas.ObtenerDetalleNoEncontrados(idNegocio, idSistema);
                modelo.ListaUsuariosDuplicados = await repositorioAlertas.ObtenerDetalleDuplicados(idNegocio, idSistema);

                // Armar cabeceras de países (excluyendo "No hay datos" para que no salga una bandera rota)
                modelo.PaisesEnCabecera = modelo.estadisticas
                    .Where(e => e.Pais != "No hay datos")
                    .Select(e => new PaisViewModel { pais = e.Pais, Bandera = e.Bandera })
                    .GroupBy(p => p.pais)
                    .Select(g => g.First())
                    .OrderBy(p => p.pais)
                    .ToList();

                // Agrupación para la vista HTML principal
                modelo.EstadisticasAgrupadas = modelo.estadisticas
                    .GroupBy(e => new { e.Vertical, e.Negocio, e.Sistema })
                    .ToList();
            }
            else
            {
                // Estado inicial vacío
                modelo.estadisticas = new List<EstadisticasUsuarios>();
                modelo.ListaFiltroFiniquitados = new List<Finiquitados>();
                modelo.ListaUsuariosNoEncontrados = new List<UsuariosNoEncontrados>();
                modelo.ListaUsuariosDuplicados = new List<UsuariosDuplicados>();
                modelo.PaisesEnCabecera = new List<PaisViewModel>();
                modelo.EstadisticasAgrupadas = new List<IGrouping<dynamic, EstadisticasUsuarios>>();
            }

            return View(modelo);
        }

        [HttpGet]
        [Monitoreo("SistemasPorNegocio", "SELECT", "obtenerSistemasPorNegocio")]
        public async Task<JsonResult> SistemasPorNegocio(int? idNegocio)
        {
            var sistemas = await repositorioAlertas.ObtenerSistemasPorNegocio(idNegocio);
            return Json(sistemas);
        }
    }
}