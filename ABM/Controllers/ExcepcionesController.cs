using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABM.Filters;
using ABM.Models;
using ABM.Servicios; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABM.Controllers
{
    [Authorize]
    public class ExcepcionesController : Controller
    {
        private readonly IRepositorioExcepciones _repositorioExcepciones;
        private readonly IRepositorioMatrizDiaria _repositorioMatrizDiaria;
        private readonly IRepositorioUsuarios _repositorioUsuarios; 

        public ExcepcionesController(IRepositorioExcepciones repositorioExcepciones,
                                     IRepositorioMatrizDiaria repositorioMatrizDiaria,
                                     IRepositorioUsuarios repositorioUsuarios) 
        {
            _repositorioExcepciones = repositorioExcepciones;
            _repositorioMatrizDiaria = repositorioMatrizDiaria;
            _repositorioUsuarios = repositorioUsuarios; 
        }
        [Monitoreo("Excepciones", "SELECT", "verPaginaExcepciones")]
        public async Task<IActionResult> Index()

        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
            {
                TempData["MensajePNS"] = "Por favor, seleccione un País y Negocio para continuar.";
                return RedirectToAction("PnsSelectorPartial", "Home");
            }

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            ViewBag.e = TempData["RespuestaE"];
            ViewBag.FilterIdPais = idPais;
            ViewBag.FilterIdNegocio = idNegocio;

            IEnumerable<SistemaExcepcion> modelo = await _repositorioExcepciones.ObtenerListaSistemaConExcepcion(idPais, idNegocio);
            return View(modelo);
        }

        [HttpGet]
        [Route("Excepciones/ListaExcepciones")]
        [Monitoreo("Excepciones", "SELECT", "verListaExcepciones")]
        public async Task<IActionResult> ListaExcepciones()
        {
            var idPaisSesion = HttpContext.Session.GetInt32("IdPais");
            var idNegocioSesion = HttpContext.Session.GetInt32("IdNegocio");

            if (!idPaisSesion.HasValue || !idNegocioSesion.HasValue)
            {
                TempData["MensajePNS"] = "Por favor, seleccione un País y Negocio para continuar.";
                return RedirectToAction("PnsSelectorPartial", "Home");
            }

            int idPais = idPaisSesion.Value;
            int idNegocio = idNegocioSesion.Value;

            ViewBag.TIPOS_EXEPCION = await _repositorioExcepciones.ObtenerTiposExcepciones();
            ViewBag.FilterIdPais = idPais;
            ViewBag.FilterIdNegocio = idNegocio;

            var modelo = await _repositorioExcepciones.ObtenerListaDetalleExcepcionPorIdSistema(idPais, idNegocio);
            return View(modelo);
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
            return r.Replace(name, "_");
        }

        [HttpPost]
        [Monitoreo("Excepciones", "INSERT", "guardarListaExcepciones")]
        public async Task<IActionResult> GuardarListaExcepciones(string IdsCarga,
            string Comentario, IFormFile Archivo,
            int IdTipoMotivo, DateTime FechaHasta, string Estado)
        {
            try
            {
                if (!HttpContext.User.Identity.IsAuthenticated)
                    return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "Usuario no autenticado" });

                string userIdString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "No se pudo identificar al usuario logueado." });
                }

                int? idRolDesdeBD = await _repositorioUsuarios.ObtenerIdRolDeUsuario(userId);
                if (!idRolDesdeBD.HasValue)
                {
                    Console.WriteLine($"Advertencia: No se encontró idRol para el usuario {userId} en ftc_usuario. Usando rol por defecto 1.");
                    idRolDesdeBD = 1; 
                }
                int roleId = idRolDesdeBD.Value; 

                Console.WriteLine($"Usuario ID: {userId}, Rol ID obtenido de BD: {roleId}");


                if (string.IsNullOrEmpty(IdsCarga))
                    return Json(new { RESPUESTA = false, TIPO = 5, MENSAJE = "No se proporcionaron IDs de carga" });

                string[] IDS = IdsCarga.Split(',');
                List<string> erroresProcesamiento = new List<string>();

                DateTime? fechaAutorizacionParaComentario = null;
                if (FechaHasta != DateTime.MinValue && FechaHasta >= (DateTime)SqlDateTime.MinValue.Value && FechaHasta <= (DateTime)SqlDateTime.MaxValue.Value)
                {
                    fechaAutorizacionParaComentario = FechaHasta.Date;
                }

                DateTime fechaHastaParaMatriz;
                if (FechaHasta != DateTime.MinValue && FechaHasta >= new DateTime(1900, 1, 1) && FechaHasta <= (DateTime)SqlDateTime.MaxValue.Value)
                {
                    fechaHastaParaMatriz = FechaHasta.Date;
                }
                else
                {
                    fechaHastaParaMatriz = new DateTime(1900, 1, 1);
                }

                Console.WriteLine($"GuardarListaExcepciones - FechaHasta recibida del form: {FechaHasta:o}");
                Console.WriteLine($"GuardarListaExcepciones - fechaAutorizacionParaComentario (para ftc_comentarios): {(fechaAutorizacionParaComentario.HasValue ? fechaAutorizacionParaComentario.Value.ToString("o") : "NULL")}");
                Console.WriteLine($"GuardarListaExcepciones - fechaHastaParaMatriz (para Actualizarcl_matriz_diaria): {fechaHastaParaMatriz:o}");


                foreach (string IdCargaStr in IDS)
                {
                    if (!int.TryParse(IdCargaStr, out int idCargaActual))
                    {
                        erroresProcesamiento.Add($"ID de carga '{IdCargaStr}' no es válido.");
                        continue;
                    }

                    try
                    {
                        var Matriz = await _repositorioMatrizDiaria.Obtenercl_matriz_diariaPorIdCarga(idCargaActual);
                        if (Matriz == null)
                            throw new Exception($"No se encontró la matriz con ID: {idCargaActual}");

                        var Motivo = await _repositorioExcepciones.ObtenerTiposExecepcionesPorId(IdTipoMotivo);
                        if (Motivo == null)
                            throw new Exception($"No se encontró el motivo con ID: {IdTipoMotivo}");

                        string NombreArchivo = string.Empty;
                        if (Archivo != null && Archivo.Length > 0)
                        {
                            string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                            string extension = Path.GetExtension(Archivo.FileName).ToLower();

                            if (!allowedExtensions.Contains(extension))
                                throw new Exception($"Tipo de archivo no permitido: {extension}");

                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ExcepcionesEvidencias");
                            Directory.CreateDirectory(uploadsFolder);

                            string sanitizedLlaveEx = SanitizeFileName(Matriz.llave_ex ?? "sin_llave");
                            string uniqueFileName = $"{Matriz.idCarga}_{sanitizedLlaveEx}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await Archivo.CopyToAsync(fileStream);
                            }
                            NombreArchivo = uniqueFileName;
                        }

                        var nuevaExcepcion = new Comentarios
                        {
                            idCarga = Matriz.idCarga,
                            idUsuario = userId,
                            idRol = roleId, 
                            idMotivo = IdTipoMotivo,
                            estado = Estado,
                            evidencia = NombreArchivo,
                            fecha_autorizacion = fechaAutorizacionParaComentario,
                            fecha_creacion = DateTime.Now.Date,
                            llave_ex = Matriz.llave_ex,
                            comentario = Comentario ?? string.Empty
                        };

                        Console.WriteLine($"GuardarListaExcepciones - Intentando guardar Comentario para IdCarga {idCargaActual}: {JsonSerializer.Serialize(nuevaExcepcion)}");
                        await _repositorioExcepciones.GuardarComentariosExcepcion(nuevaExcepcion);
                        Console.WriteLine($"GuardarListaExcepciones - Comentario para IdCarga {idCargaActual} guardado exitosamente.");

                        Console.WriteLine($"GuardarListaExcepciones - Intentando actualizar Matriz Diaria para IdCarga {idCargaActual} con FechaHasta: {fechaHastaParaMatriz:o}");
                        await _repositorioMatrizDiaria.Actualizarcl_matriz_diariaPorExcepcion(
                            Matriz,
                            fechaHastaParaMatriz,
                            Motivo.nombreMotivo,
                            Comentario
                        );
                        Console.WriteLine($"GuardarListaExcepciones - Matriz Diaria para IdCarga {idCargaActual} actualizada exitosamente.");

                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Error procesando la carga {IdCargaStr}: {ex.Message}";
                        if (ex.InnerException != null)
                        {
                            errorMessage += $" (Inner Exception: {ex.InnerException.Message})";
                        }
                        Console.WriteLine($"{errorMessage}\nStackTrace: {ex.StackTrace}");
                        erroresProcesamiento.Add(errorMessage);
                    }
                }

                if (erroresProcesamiento.Any())
                {
                    return Json(new { RESPUESTA = false, TIPO = 5, MENSAJE = "Algunas excepciones no se procesaron.", ERRORES = erroresProcesamiento });
                }

                TempData["RespuestaE"] = 3;
                return Json(new { RESPUESTA = true, TIPO = 1, MENSAJE = "Excepciones guardadas correctamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GuardarListaExcepciones - Error General: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { RESPUESTA = false, TIPO = 5, ERROR = ex.Message, DETALLES = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        [Monitoreo("Excepciones", "INSERT", "guardarExcepcionConEnvioCorreo")]
        public async Task<IActionResult> GuardarExcepcionConEnvioCorreo(int idCarga, string observaciones, string correoEnvio)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "Usuario no autenticado" });
            }

            try
            {
                var matriz = await _repositorioMatrizDiaria.Obtenercl_matriz_diariaPorIdCarga(idCarga);
                if (matriz == null)
                {
                    return Json(new { RESPUESTA = false, TIPO = 2, MENSAJE = "No se encontró la matriz diaria" });
                }

                Console.WriteLine("Llamada a _repositorioMatrizDiaria.GuardarExcepcionConEnvioCorreo COMENTADA - VERIFICAR EXISTENCIA");

                TempData["RespuestaE"] = 3;
                return Json(new { RESPUESTA = true, TIPO = 1, MENSAJE = "Operación procesada (verificar lógica de envío de correo)." });
            }
            catch (Exception ex)
            {
                return Json(new { RESPUESTA = false, TIPO = 3, MENSAJE = "Error al procesar la solicitud: " + ex.Message });
            }
        }

        [HttpGet]
        [Monitoreo("ExcepcionesHistoricos", "SELECT", "verPaginaHistoricosExcepciones")]
        public IActionResult Historicos()
        {
            return View();
        }

        [HttpGet]
        [Monitoreo("ExcepcionesNotificadas", "SELECT", "verExcepcionesNotificadas")]
        public async Task<IActionResult> ExcepcionesNotificadas()
        {
            Console.WriteLine("Llamada a _repositorioMatrizDiaria.ObtenerExcepcionesNotificadas COMENTADA - VERIFICAR EXISTENCIA");
            var excepcionesNotificadas = new List<object>();
            return View(excepcionesNotificadas);
        }

        [HttpPost]
        [Monitoreo("ExcepcionesHistoricos", "SELECT", "obtenerListaExcepcionesHistoricos")]
        public async Task<IActionResult> ListaExcepcionesHistoricos()
        {
            try
            {
                string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idUsuarioString) || !int.TryParse(idUsuarioString, out int idUsuario))
                {
                    return Json(new { error = "Usuario no identificado" });
                }

                List<Comentarios> excepciones = await _repositorioExcepciones.ObtenerListaHistoricaExcepcionesPorUsuario(idUsuario);

                var search = HttpContext.Request.Form["search[value]"].ToString();
                var draw = HttpContext.Request.Form["draw"].ToString();
                var orderColumnIndex = HttpContext.Request.Form["order[0][column]"].ToString(); 
                var orderDir = HttpContext.Request.Form["order[0][dir]"].ToString();
                int startRec = int.Parse(HttpContext.Request.Form["start"]);
                int pageSize = int.Parse(HttpContext.Request.Form["length"]);

                int totalRecords = excepciones.Count;

                if (!string.IsNullOrEmpty(search) && !string.IsNullOrWhiteSpace(search))
                {
                    string searchLower = search.ToLower();
                    excepciones = excepciones.Where(p =>
                        (p.idCarga.ToString()?.Contains(searchLower) ?? false) ||
                        (p.Responsable?.ToLower().Contains(searchLower) ?? false) ||
                        (p.motivo?.ToLower().Contains(searchLower) ?? false) ||
                        (p.estado?.ToLower().Contains(searchLower) ?? false) ||
                        (p.comentario?.ToLower().Contains(searchLower) ?? false) ||
                        (p.fecha_autorizacion?.ToString("dd-MM-yyyy").Contains(searchLower) ?? false) ||
                        (p.fecha_creacion?.ToString("dd-MM-yyyy").Contains(searchLower) ?? false) ||
                        (p.nombreusuario?.ToLower().Contains(searchLower) ?? false) ||
                        (p.cargospr?.ToLower().Contains(searchLower) ?? false) ||
                        (p.perfil?.ToLower().Contains(searchLower) ?? false) ||
                        (p.Pais?.ToLower().Contains(searchLower) ?? false) ||   
                        (p.Negocio?.ToLower().Contains(searchLower) ?? false)    
                    ).ToList();
                }

                string orderColumnName = "idCarga"; 
                switch (orderColumnIndex)
                {
                    case "0": orderColumnName = "idCarga"; break;
                    case "1": orderColumnName = "motivo"; break;
                    case "2": orderColumnName = "Responsable"; break;
                    case "3": orderColumnName = "estado"; break;
                    case "4": orderColumnName = "comentario"; break;
                    // case "5" es Evidencia, no se ordena usualmente.
                    case "6": orderColumnName = "fecha_autorizacion"; break;
                    case "7": orderColumnName = "fecha_creacion"; break;
                    case "8": orderColumnName = "nombreusuario"; break;
                    case "9": orderColumnName = "cargospr"; break;
                    case "10": orderColumnName = "perfil"; break;
                    case "11": orderColumnName = "Pais"; break;   
                    case "12": orderColumnName = "Negocio"; break;  
                }

                excepciones = SortByColumnWithOrder(orderColumnName, orderDir, excepciones);
                int recFilter = excepciones.Count;
                excepciones = excepciones.Skip(startRec).Take(pageSize).ToList();

                return Json(new { draw = Convert.ToInt32(draw), recordsTotal = totalRecords, recordsFiltered = recFilter, data = excepciones });
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return Json(new { error = "Error al procesar la solicitud.", details = ex.Message });
            }
        }

        private List<Comentarios> SortByColumnWithOrder(string orderColumnName, string orderDir, List<Comentarios> data)
        {
            try
            {
                var prop = typeof(Comentarios).GetProperty(orderColumnName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (prop == null)
                {
                    prop = typeof(Comentarios).GetProperty("idCarga");
                }

                if (orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase))
                {
                    return data.OrderByDescending(x => prop.GetValue(x, null)).ToList();
                }
                else
                {
                    return data.OrderBy(x => prop.GetValue(x, null)).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return data.OrderBy(p => p.idCarga).ToList();
            }
        }

        [HttpPost]
        [Monitoreo("ExcepcionesPorVencer", "SELECT", "obtenerExcepcionesPorVencer")]
        public async Task<IActionResult> ObtenerListaExcepcionesPorVencer()
        {
            try
            {
                string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idUsuarioString) || !int.TryParse(idUsuarioString, out int idUsuario))
                {
                    return Json(new { error = "Usuario no identificado" });
                }

                var excepciones = await _repositorioExcepciones.ObtenerListaHistoricaExcepcionesPorUsuario(idUsuario);

                var today = DateTime.Today;
                var excepcionesPorVencer = excepciones
                    .Where(e => e.fecha_autorizacion.HasValue &&
                                e.fecha_autorizacion.Value.Date > today &&
                                (e.fecha_autorizacion.Value.Date - today).TotalDays <= 5)
                    .Select(e => new
                    {
                        e.idCarga,
                        e.fecha_autorizacion,
                        e.motivo,
                        e.Responsable,
                        diasRestantes = (e.fecha_autorizacion.Value.Date - today).TotalDays
                    })
                    .ToList();

                return Json(excepcionesPorVencer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new { error = "Error al obtener las excepciones por vencer.", details = ex.Message });
            }
        }
    }
}
