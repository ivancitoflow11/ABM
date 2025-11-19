using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ABM.Filters;
using ABM.Models;
using ABM.Servicios;
using ABM.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class BusquedaMasivaController : Controller
    {
        private readonly IRepositorioConsultaUsuario _repositorio;
        private readonly IRepositorioBusquedaMasiva _repositorioMasiva;

        public BusquedaMasivaController(
            IRepositorioConsultaUsuario repositorio,
            IRepositorioBusquedaMasiva repositorioMasiva)
        {
            _repositorio = repositorio;
            _repositorioMasiva = repositorioMasiva;
        }

        [HttpGet]
        [Monitoreo("BusquedaMasiva", "SELECT", "verPaginaBusquedaMasiva")]
        public IActionResult Index()
        {
            var vm = new ViewModels.BusquedaMasivaViewModel();
            return View(vm);
        }
        [HttpGet]
        [Monitoreo("BusquedaMasivaDuro", "SELECT", "verPaginaBusquedaMasivaDuro")]
        public IActionResult DURO()
        {
            var vm = new ViewModels.BusquedaMasivaViewModel();
            return View(vm);
        }

        [HttpPost]
        [Monitoreo("BusquedaMasiva", "INSERT", "procesarArchivoBusquedaMasiva")]
        public async Task<IActionResult> ProcesarExcel(ViewModels.BusquedaMasivaViewModel vm)
        {
            if (vm.ArchivoExcel == null || vm.ArchivoExcel.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar un archivo Excel");
                return View("Index", vm);
            }

            try
            {
                var resultados = new List<ViewModels.ResultadoBusquedaMasiva>();
                var inputsUsuarios = new List<string>();

                // Leer el archivo Excel con ClosedXML
                using (var stream = new MemoryStream())
                {
                    await vm.ArchivoExcel.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                        if (rowCount <= 1)
                        {
                            ModelState.AddModelError("", "El archivo no contiene datos");
                            return View("Index", vm);
                        }

                        // Buscar la columna RUT o CORREO
                        int columnaInput = -1;
                        var headerRow = worksheet.Row(1);

                        for (int col = 1; col <= worksheet.LastColumnUsed().ColumnNumber(); col++)
                        {
                            var header = headerRow.Cell(col).GetString().Trim().ToUpper();
                            if (header == "RUT" || header == "CORREO")
                            {
                                columnaInput = col;
                                break;
                            }
                        }

                        if (columnaInput == -1)
                        {
                            ModelState.AddModelError("", "El archivo debe contener una columna llamada 'RUT' o 'CORREO'");
                            return View("Index", vm);
                        }

                        // Leer los datos de la columna
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var input = worksheet.Cell(row, columnaInput).GetString().Trim();
                            if (!string.IsNullOrWhiteSpace(input))
                            {
                                inputsUsuarios.Add(input);
                            }
                        }
                    }
                }

                if (!inputsUsuarios.Any())
                {
                    ModelState.AddModelError("", "No se encontraron datos válidos en el archivo");
                    return View("Index", vm);
                }

                // Limitar a 100 registros
                if (inputsUsuarios.Count > 100)
                {
                    ModelState.AddModelError("", "El archivo contiene más de 100 registros. Por favor, procese máximo 100 a la vez.");
                    return View("Index", vm);
                }

                // Crear el registro de búsqueda en la base de datos
                var idBusqueda = await _repositorioMasiva.CrearBusqueda(
                    User.Identity?.Name ?? "Sistema",
                    vm.ArchivoExcel.FileName,
                    inputsUsuarios.Count
                );

                // Procesar cada usuario
                int registrosConProblemas = 0;

                foreach (var input in inputsUsuarios)
                {
                    var resultado = new ViewModels.ResultadoBusquedaMasiva
                    {
                        InputBusqueda = input
                    };

                    try
                    {
                        // Buscar datos básicos
                        var usuario = await _repositorio.ObtenerDatosBasicos(input);

                        if (usuario == null && input.Contains(" "))
                        {
                            var coincidencias = await _repositorio.BuscarCoincidencias(input);
                            usuario = coincidencias.FirstOrDefault();
                        }

                        if (usuario != null)
                        {
                            resultado.Rut = usuario.Rut;
                            resultado.Nombre = usuario.Nombre;
                            resultado.Correo = usuario.Correo;

                            // Obtener estado AD
                            var datosAD = await _repositorio.ObtenerDatosAD(usuario.Correo, usuario.Rut);
                            resultado.EstadoAD = datosAD != null ? "ACTIVO" : "NO ENCONTRADO";
                            resultado.UltimoLoginAD = datosAD?.ultimo_login;

                            // Obtener estado finiquitado
                            var claveFiniq = string.IsNullOrWhiteSpace(usuario.Rut) ? input : usuario.Rut;
                            var datosFiniquito = await _repositorio.ObtenerDatosFiniquito(claveFiniq);
                            resultado.EsFiniquitado = datosFiniquito != null;
                            resultado.FechaFiniquito = datosFiniquito?.fecfiniquito;

                            // Obtener estado SPR y Emp Central
                            var claveSpr = !string.IsNullOrWhiteSpace(usuario.Rut) ? usuario.Rut
                                          : (!string.IsNullOrWhiteSpace(usuario.Correo) ? usuario.Correo : input);
                            var (spr, emp) = await _repositorio.ObtenerEstadoSprEmpCentral(claveSpr);
                            resultado.SprActivo = spr;
                            resultado.EmpCentralActivo = emp;

                            // Obtener sistemas
                            var sistemas = await _repositorio.ObtenerSistemas(claveSpr);
                            resultado.Sistemas = sistemas.ToList();

                            // 👇 LOGS DE DEPURACIÓN
                            System.Diagnostics.Debug.WriteLine($"\n=== DEBUG Usuario: {input} | Clave: {claveSpr} ===");
                            System.Diagnostics.Debug.WriteLine($"Sistemas encontrados: {sistemas.Count()}");
                            foreach (var s in sistemas)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Sistema: '{s.sistema}' | Estado: '{s.estado}' | NegocioPais: '{s.NegocioPais}'");
                            }

                            // Obtener negocios únicos
                            resultado.Negocios = sistemas
                                .Where(s => !string.IsNullOrWhiteSpace(s.NegocioPais))
                                .Select(s => s.NegocioPais)
                                .Distinct()
                                .OrderBy(n => n)
                                .ToList();

                            System.Diagnostics.Debug.WriteLine($"Negocios únicos: {resultado.Negocios.Count}");
                            foreach (var n in resultado.Negocios)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Negocio: {n}");
                            }
                            System.Diagnostics.Debug.WriteLine($"Resultado.Sistemas.Count: {resultado.Sistemas.Count}");
                            System.Diagnostics.Debug.WriteLine($"Resultado.Negocios.Count: {resultado.Negocios.Count}");
                            System.Diagnostics.Debug.WriteLine("=== FIN DEBUG ===\n");

                            // Detectar problemas
                            resultado.TieneProblemas = resultado.EsFiniquitado && (resultado.SprActivo || resultado.EmpCentralActivo);

                            if (resultado.TieneProblemas)
                                registrosConProblemas++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log del error
                        System.Diagnostics.Debug.WriteLine($"ERROR procesando usuario {input}: {ex.Message}");
                        resultado.TieneProblemas = true;
                        registrosConProblemas++;
                    }

                    resultados.Add(resultado);

                    // Guardar en la base de datos
                    var detalle = new DetalleResultadoBusqueda
                    {
                        InputBusqueda = resultado.InputBusqueda,
                        RutEncontrado = resultado.Rut,
                        NombreEncontrado = resultado.Nombre,
                        CorreoEncontrado = resultado.Correo,
                        EstadoAD = resultado.EstadoAD,
                        UltimoLoginAD = resultado.UltimoLoginAD,
                        EsFiniquitado = resultado.EsFiniquitado,
                        FechaFiniquito = resultado.FechaFiniquito,
                        SprActivo = resultado.SprActivo,
                        EmpCentralActivo = resultado.EmpCentralActivo,
                        JsonSistemas = JsonSerializer.Serialize(resultado.Sistemas),
                        JsonNegocios = JsonSerializer.Serialize(resultado.Negocios),
                        TieneProblemas = resultado.TieneProblemas
                    };

                    await _repositorioMasiva.GuardarDetalle(idBusqueda, detalle);
                }

                // Actualizar estado de la búsqueda
                await _repositorioMasiva.ActualizarEstadoBusqueda(idBusqueda, "Completado");

                // Preparar el viewmodel con los resultados
                vm.IdBusqueda = idBusqueda;
                vm.Resultados = resultados;
                vm.TotalRegistros = resultados.Count;
                vm.RegistrosConProblemas = registrosConProblemas;
                vm.Estado = "Completado";

                // 👇 LOG FINAL
                System.Diagnostics.Debug.WriteLine($"\n=== RESULTADO FINAL ===");
                System.Diagnostics.Debug.WriteLine($"Total resultados: {vm.Resultados.Count}");
                foreach (var r in vm.Resultados)
                {
                    System.Diagnostics.Debug.WriteLine($"Usuario: {r.InputBusqueda} - Sistemas: {r.Sistemas?.Count ?? 0} - Negocios: {r.Negocios?.Count ?? 0}");
                }
                System.Diagnostics.Debug.WriteLine("=== FIN RESULTADO FINAL ===\n");

                // 👇 LOG JUSTO ANTES DE ENVIAR A LA VISTA
                System.Diagnostics.Debug.WriteLine($"\n=== VERIFICACIÓN ANTES DE VISTA ===");
                System.Diagnostics.Debug.WriteLine($"vm.Resultados.Count: {vm.Resultados?.Count}");
                if (vm.Resultados != null)
                {
                    foreach (var res in vm.Resultados)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {res.InputBusqueda}: Sistemas={res.Sistemas?.Count ?? -1}, Negocios={res.Negocios?.Count ?? -1}");
                        if (res.Sistemas != null && res.Sistemas.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"    Primer sistema: {res.Sistemas[0].sistema} - NegocioPais: {res.Sistemas[0].NegocioPais}");
                            foreach (var sis in res.Sistemas)
                            {
                                System.Diagnostics.Debug.WriteLine($"      Sistema: {sis.sistema}");
                            }
                        }
                        if (res.Negocios != null && res.Negocios.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"    Primer negocio: {res.Negocios[0]}");
                            foreach (var neg in res.Negocios)
                            {
                                System.Diagnostics.Debug.WriteLine($"      Negocio: {neg}");
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine("=== FIN VERIFICACIÓN ===\n");

                return View("Index", vm);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar el archivo: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ERROR GENERAL: {ex.Message}\n{ex.StackTrace}");
                return View("Index", vm);
            }
        }
    }
}