using ABM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ABM.Servicios;
using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ABM.Controllers
{
    [Authorize]
    public class ExcepcionesController : Controller
    {
        private readonly IRepositorioExcepciones _repositorioExcepciones;
        private readonly IRepositorioMatrizDiaria _repositorioMatrizDiaria;

        public ExcepcionesController(IRepositorioExcepciones repositorioExcepciones,
                                     IRepositorioMatrizDiaria repositorioMatrizDiaria)
        {
            _repositorioExcepciones = repositorioExcepciones;
            _repositorioMatrizDiaria = repositorioMatrizDiaria;
        }

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
            ViewBag.FilterIdPais = idPais; // Para la vista, si es necesario
            ViewBag.FilterIdNegocio = idNegocio; // Para la vista, si es necesario

            var modelo = await _repositorioExcepciones.ObtenerListaDetalleExcepcionPorIdSistema(idPais, idNegocio);
            return View(modelo);
        }


        [HttpPost]
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

                string roleIdString = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                int roleId = !string.IsNullOrEmpty(roleIdString) ? int.Parse(roleIdString) : 1;

                if (string.IsNullOrEmpty(IdsCarga))
                    return Json(new { RESPUESTA = false, TIPO = 5, MENSAJE = "No se proporcionaron IDs de carga" });

                string[] IDS = IdsCarga.Split(',');
                List<string> erroresProcesamiento = new List<string>();

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

                            string uniqueFileName = $"{Matriz.idCarga}_{Matriz.llave_ex}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
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
                            fecha_autorizacion = FechaHasta,
                            fecha_creacion = DateTime.Now,
                            llave_ex = Matriz.llave_ex,
                            comentario = Comentario ?? string.Empty
                        };

                        await _repositorioExcepciones.GuardarComentariosExcepcion(nuevaExcepcion);
                        await _repositorioMatrizDiaria.Actualizarcl_matriz_diariaPorExcepcion(
                            Matriz,
                            FechaHasta,
                            Motivo.nombreMotivo,
                            Comentario
                        );
                    }
                    catch (Exception ex)
                    {
                        erroresProcesamiento.Add($"Error procesando la carga {IdCargaStr}: {ex.Message}");
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
                return Json(new { RESPUESTA = false, TIPO = 5, ERROR = ex.Message, DETALLES = ex.InnerException?.Message });
            }
        }

        [HttpPost]
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
        public IActionResult Historicos()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExcepcionesNotificadas()
        {
            Console.WriteLine("Llamada a _repositorioMatrizDiaria.ObtenerExcepcionesNotificadas COMENTADA - VERIFICAR EXISTENCIA");
            var excepcionesNotificadas = new List<object>();
            return View(excepcionesNotificadas);
        }

        [HttpPost]
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
                var order = HttpContext.Request.Form["order[0][column]"].ToString();
                var orderDir = HttpContext.Request.Form["order[0][dir]"].ToString();
                int startRec = int.Parse(HttpContext.Request.Form["start"]);
                int pageSize = int.Parse(HttpContext.Request.Form["length"]);

                int totalRecords = excepciones.Count;

                if (!string.IsNullOrEmpty(search) && !string.IsNullOrWhiteSpace(search))
                {
                    excepciones = excepciones.Where(p =>
                        (p.idCarga.ToString()?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.idUsuario.ToString()?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.idRol.ToString()?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.Responsable?.ToString().ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.motivo?.ToString().ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.estado?.ToString().ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.comentario?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.evidencia?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.fecha_autorizacion.ToString()?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.fecha_creacion.ToString()?.ToLower().Contains(search.ToLower()) ?? false) ||
                        (p.llave_ex?.ToString().ToLower().Contains(search.ToLower()) ?? false)
                    ).ToList();
                }

                excepciones = SortByColumnWithOrder(order, orderDir, excepciones);
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

        private List<Comentarios> SortByColumnWithOrder(string order, string orderDir, List<Comentarios> data)
        {
            List<Comentarios> lst = new List<Comentarios>();
            try
            {
                switch (order)
                {
                    case "0": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.idCarga).ToList() : data.OrderBy(p => p.idCarga).ToList(); break;
                    case "1": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.motivo).ToList() : data.OrderBy(p => p.motivo).ToList(); break;
                    case "2": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.estado).ToList() : data.OrderBy(p => p.estado).ToList(); break;
                    case "3": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.comentario).ToList() : data.OrderBy(p => p.comentario).ToList(); break;
                    case "4": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.evidencia).ToList() : data.OrderBy(p => p.evidencia).ToList(); break;
                    case "5": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.fecha_autorizacion).ToList() : data.OrderBy(p => p.fecha_autorizacion).ToList(); break;
                    case "6": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.fecha_creacion).ToList() : data.OrderBy(p => p.fecha_creacion).ToList(); break;
                    case "7": lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.llave_ex).ToList() : data.OrderBy(p => p.llave_ex).ToList(); break;
                    default: lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.idCarga).ToList() : data.OrderBy(p => p.idCarga).ToList(); break;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return data;
            }
            return lst;
        }

        [HttpPost]
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
