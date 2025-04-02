using ABM.Data;
using ABM.Models;
using ABM.Servicios;
using ABM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ABM.Controllers
{
	[Authorize]
	public class ExcepcionesController : Controller
	{
		private readonly IRepositorioExcepciones repositorioExcepciones;
		private readonly IRepositorioMatrizDiaria repositorioMatrizDiaria;
		private readonly HttpContext httpContext;
        private readonly AppDBContext _appDBContext;

        public ExcepcionesController(IRepositorioExcepciones RepositorioExcepciones, IRepositorioMatrizDiaria RepositorioMatrizDiaria, IHttpContextAccessor httpContextAccessor, AppDBContext appDBContext)
		{
			repositorioExcepciones = RepositorioExcepciones;
			repositorioMatrizDiaria = RepositorioMatrizDiaria;
			httpContext = httpContextAccessor.HttpContext;
            _appDBContext = appDBContext;

        }
		public async Task<IActionResult> Index()
		{
			ViewBag.e = TempData["RespuestaE"];
			IEnumerable<SistemaExcepcion> modelo = await repositorioExcepciones.ObtenerListaSistemaConExcepcion();
			return View(modelo);
		}

        [HttpGet]
        [Route("Excepciones/ListaExcepciones/{Id}")]
        public async Task<IActionResult> ListaExcepciones(int Id)
        {
            string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int idUsuario;
            if (int.TryParse(idUsuarioString, out idUsuario))
            {
                var usuario = await _appDBContext.Usuario
                    .Include(u => u.Gerencia) // Incluir la gerencia asociada
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

                if (usuario != null)
                {
                    bool tieneGerencia = usuario.ID_gerencia != null;
                    ViewBag.TieneGerencia = tieneGerencia;

                    // Obtener tipos de excepciones antes de devolver la vista
                    ViewBag.TIPOS_EXEPCION = await repositorioExcepciones.ObtenerTiposExcepciones();

                    if (!tieneGerencia)
                    {
                        var modelo = await repositorioExcepciones.ObtenerListaDetalleExcepcionPorIdSistema(Id);
                        return View(modelo);
                    }
                    else
                    {
                        var modelo = await repositorioExcepciones.ObtenerListaDetalleExcepcionPorGerencia(Id, usuario.IdUsuario);
                        return View(modelo);
                    }
                }
                else
                {
                    return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
                }
            }
            else
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Id de usuario no válido." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> GuardarListaExcepciones(string IdsCarga,
    string Comentario, IFormFile Archivo,
    int IdTipoMotivo, DateTime FechaHasta, string Estado)
        {
            try
            {
                // Validación inicial
                if (!HttpContext.User.Identity.IsAuthenticated)
                    return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "Usuario no autenticado" });

                // Obtener el ID del usuario logueado
                string userIdString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "No se pudo identificar al usuario logueado." });
                }

                // Obtener el rol del usuario logueado, si es necesario
                string roleIdString = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                int roleId = !string.IsNullOrEmpty(roleIdString) ? int.Parse(roleIdString) : 1; // Default al rol 1 si no está definido

                // Validar IDs de carga
                if (string.IsNullOrEmpty(IdsCarga))
                    return Json(new { RESPUESTA = false, TIPO = 5, MENSAJE = "No se proporcionaron IDs de carga" });


                string[] IDS = IdsCarga.Split(',');
                foreach (string IdCarga in IDS)
                {
                    try
                    {
                        var Matriz = await repositorioMatrizDiaria.Obtenercl_matriz_diariaPorIdCarga(Convert.ToInt32(IdCarga));
                        if (Matriz == null)
                            throw new Exception($"No se encontró la matriz con ID: {IdCarga}");

                        var Motivo = await repositorioExcepciones.ObtenerTiposExecepcionesPorId(IdTipoMotivo);
                        if (Motivo == null)
                            throw new Exception($"No se encontró el motivo con ID: {IdTipoMotivo}");

                        string NombreArchivo = string.Empty;
                        if (Archivo != null && Archivo.Length > 0)
                        {
                            try
                            {
                                string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                                string extension = Path.GetExtension(Archivo.FileName).ToLower();

                                if (!allowedExtensions.Contains(extension))
                                    throw new Exception($"Tipo de archivo no permitido: {extension}");

                                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ExcepcionesEvidencias");
                                Directory.CreateDirectory(uploadsFolder); // Seguro incluso si el directorio ya existe

                                string uniqueFileName = $"{Matriz.idCarga}_{Matriz.llave_ex}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await Archivo.CopyToAsync(fileStream);
                                }

                                NombreArchivo = uniqueFileName;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error al procesar el archivo: {ex.Message}");
                            }
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

                        try
                        {
                            await repositorioExcepciones.GuardarComentariosExcepcion(nuevaExcepcion);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error al guardar la excepción en la base de datos: {ex.Message}");
                        }

                        try
                        {
                            await repositorioMatrizDiaria.Actualizarcl_matriz_diariaPorExcepcion(
                                Matriz,
                                FechaHasta,
                                Motivo.nombreMotivo,
                                Comentario
                            );
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error al actualizar la matriz diaria: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error procesando la carga {IdCarga}: {ex.Message}");
                    }
                }

                TempData["RespuestaE"] = 3;
                return Json(new { RESPUESTA = true, TIPO = 1 });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    RESPUESTA = false,
                    TIPO = 5,
                    ERROR = ex.Message,
                    DETALLES = ex.InnerException?.Message
                });
            }
        }


        [HttpPost]
		public async Task<IActionResult> GuardarExcepcionConEnvioCorreo(int idCarga, string observaciones, string correoEnvio)
		{
			if (HttpContext.User.Identity.IsAuthenticated)
			{
				try
				{
					// Obtener el usuario actual
					ClaimsPrincipal userPrincipal = HttpContext.User;
					string userName = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

					// Obtener la matriz diaria por idCarga
					var matriz = await repositorioMatrizDiaria.Obtenercl_matriz_diariaPorIdCarga(idCarga);

					if (matriz == null)
					{
						return Json(new { RESPUESTA = false, TIPO = 2, MENSAJE = "No se encontró la matriz diaria" });
					}

					// Guardar la excepción con envío de correo
					await repositorioMatrizDiaria.GuardarExcepcionConEnvioCorreo(matriz, observaciones, correoEnvio);

					TempData["RespuestaE"] = 3;
					return Json(new { RESPUESTA = true, TIPO = 1, MENSAJE = "Excepción guardada correctamente" });
				}
				catch (Exception ex)
				{
					// Loguear el error
					// logger.LogError(ex, "Error al guardar excepción con envío de correo");

					return Json(new { RESPUESTA = false, TIPO = 3, MENSAJE = "Error al procesar la solicitud: " + ex.Message });
				}
			}

			return Json(new { RESPUESTA = false, TIPO = 4, MENSAJE = "Usuario no autenticado" });
		}


		[HttpGet]
		public IActionResult Historicos()
		{
			return View();
		}

        [HttpGet]
        public async Task<IActionResult> ExcepcionesNotificadas()
        {
            var excepcionesNotificadas = await repositorioMatrizDiaria.ObtenerExcepcionesNotificadas();
            return View(excepcionesNotificadas);
        }

        [HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ListaExcepcionesHistoricos()
		{
			try
			{
                // Obtener el ID del usuario autenticado
                string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idUsuarioString) || !int.TryParse(idUsuarioString, out int idUsuario))
                {
                    return Json(new { error = "Usuario no identificado" });
                }

                // Obtener el usuario y su gerencia
                var usuario = await _appDBContext.Usuario
                    .Include(u => u.Gerencia) // Incluir la gerencia asociada
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

                if (usuario == null)
                {
                    return Json(new { error = "Usuario no encontrado" });
                }
                //// Initialization.
                //string search = Request.Form.GetValues("search[value]")[0];
                //string draw = Request.Form.GetValues("draw")[0];
                //string order = Request.Form.GetValues("order[0][column]")[0];
                //string orderDir = Request.Form.GetValues("order[0][dir]")[0];
                //int startRec = Convert.ToInt32(Request.Form.GetValues("start")[0]);
                //int pageSize = Convert.ToInt32(Request.Form.GetValues("length")[0]);

                var search = httpContext.Request.Form["search[value]"].ToString();
				var draw = httpContext.Request.Form["draw"].ToString();
				var order = httpContext.Request.Form["order[0][column]"].ToString();
				var orderDir = httpContext.Request.Form["order[0][dir]"].ToString();
				int startRec = int.Parse(httpContext.Request.Form["start"]);
				int pageSize = int.Parse(httpContext.Request.Form["length"]);

				if (order == "0")
				{
					order = "0";
				}
                // Loading.
                // Obtener lista según si tiene gerencia o no
                List<Comentarios> Excepciones;
                if (usuario.ID_gerencia == null) // Si no tiene gerencia asignada
                {
                    Excepciones = await repositorioExcepciones.ObtenerListaHistoricaExcepciones();
                }
                else // Si tiene gerencia, solo las excepciones de su gerencia
                {
                    Excepciones = await repositorioExcepciones.ObtenerListaHistoricaExcepcionesPorUsuario(usuario.IdUsuario);
                }

                //List<SP_TICKET_HISTORICOS_Result> data = db.SP_TICKET_HISTORICOS().ToList();

                // Total record count.
                int totalRecords = Excepciones.Count;

				// Verification.
				if (!string.IsNullOrEmpty(search) &&
					!string.IsNullOrWhiteSpace(search))
				{
                    // Apply search
                    Excepciones = Excepciones.Where(p =>
                                                    p.idCarga.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.idUsuario.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.idRol.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.Responsable.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.motivo.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.estado.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.comentario.ToLower().Contains(search.ToLower()) ||
                                                    p.evidencia.ToLower().Contains(search.ToLower()) ||
                                                    p.fecha_autorizacion.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.fecha_creacion.ToString().ToLower().Contains(search.ToLower()) ||
                                                    p.llave_ex.ToString().ToLower().Contains(search.ToLower()) 
                                                ).ToList();
                }

				// Sorting.
				Excepciones = this.SortByColumnWithOrder(order, orderDir, Excepciones);

				// Filter record count.
				int recFilter = Excepciones.Count;

				// Apply pagination.
				Excepciones = Excepciones.Skip(startRec).Take(pageSize).ToList();

				// Loading drop down lists.
				return Json(new { draw = Convert.ToInt32(draw), recordsTotal = totalRecords, recordsFiltered = recFilter, data = Excepciones });


			}
			catch (Exception ex)
			{
				// Info
				Console.Write(ex);
			}

			// Return info.
			return Json(new { /* your data here */ });

		}

		private List<Comentarios> SortByColumnWithOrder(string order, string orderDir, List<Comentarios> data)
		{
			// Initialization.
			List<Comentarios> lst = new List<Comentarios>();

			try
			{
				// Sorting
				switch (order)
				{
					case "0":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.idCarga).ToList()
																								 : data.OrderBy(p => p.idCarga).ToList();
						break;

					case "1":
						// Setting.

						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.motivo).ToList()
																								 : data.OrderBy(p => p.motivo).ToList();
						break;

					case "2":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.estado).ToList()
																								 : data.OrderBy(p => p.estado).ToList();
						break;

					case "3":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.comentario).ToList()
																								 : data.OrderBy(p => p.comentario).ToList();
						break;

					case "4":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.evidencia).ToList()
																								   : data.OrderBy(p => p.evidencia).ToList();
						break;

					case "5":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.fecha_autorizacion).ToList()
																								 : data.OrderBy(p => p.fecha_autorizacion).ToList();
						break;

					case "6":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.fecha_creacion).ToList()
																								 : data.OrderBy(p => p.fecha_creacion).ToList();
						break;
					case "7":
						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.llave_ex).ToList()
																								 : data.OrderBy(p => p.llave_ex).ToList();
						break;

					default:

						// Setting.
						lst = orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase) ? data.OrderByDescending(p => p.idCarga).ToList()
																								 : data.OrderBy(p => p.idCarga).ToList();
						break;
				}
			}
			catch (Exception ex)
			{
				// info.
				Console.Write(ex);
			}

			// info.
			return lst;
		}

        [HttpPost]
        public async Task<IActionResult> ObtenerListaExcepcionesPorVencer()
        {
            try
            {
                // Obtener el ID del usuario autenticado
                string idUsuarioString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idUsuarioString) || !int.TryParse(idUsuarioString, out int idUsuario))
                {
                    return Json(new { error = "Usuario no identificado" });
                }

                // Obtener el usuario y su gerencia
                var usuario = await _appDBContext.Usuario
                    .Include(u => u.Gerencia)
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

                if (usuario == null)
                {
                    return Json(new { error = "Usuario no encontrado" });
                }

                // Si el usuario no tiene gerencia, no retornar excepciones
                if (usuario.ID_gerencia == null)
                {
                    return Json(new { message = "El usuario no tiene gerencia asignada." });
                }

                // Obtener la fecha actual
                var today = DateTime.Today;

                // Obtener excepciones asociadas al usuario
                var excepciones = await repositorioExcepciones.ObtenerListaHistoricaExcepcionesPorUsuario(usuario.IdUsuario);

                // Filtrar excepciones que vencerán en los próximos 5 días
                var excepcionesPorVencer = excepciones
                    .Where(e => e.fecha_autorizacion.HasValue &&
                               e.fecha_autorizacion.Value > today &&
                               (e.fecha_autorizacion.Value - today).TotalDays <= 5)
                    .Select(e => new
                    {
                        idCarga = e.idCarga,
                        fecha_autorizacion = e.fecha_autorizacion,
                        motivo = e.motivo,
                        responsable = e.Responsable,
                        nombreusuario = e.nombreusuario,
                        diasRestantes = (e.fecha_autorizacion.Value - today).TotalDays
                    })
                    .ToList();

                return Json(excepcionesPorVencer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new { error = "Error al obtener las excepciones por vencer." });
            }
        }


    }
}
