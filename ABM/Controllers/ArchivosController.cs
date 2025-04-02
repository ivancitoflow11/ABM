using ABM.Models;
using ABM.Servicios;
using ABM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using ExcelDataReader;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ABM.Data;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Rotativa.AspNetCore;

namespace ABM.Controllers
{
    [Authorize]
    public class ArchivosController : Controller
    {
        private readonly AppDBContext _appDBContext;
        private readonly IRepositorioCargas repositorioCargas;
        private readonly IRepositorioExcepciones repositorioExcepciones; // Agregado
        private readonly string connectionString;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ArchivosController> _logger;

        public ArchivosController(
            AppDBContext appDBContext,
            IRepositorioCargas RepositorioCargas,
            IRepositorioExcepciones repositorioExcepciones, // Agregado
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ArchivosController> logger)
        {
            _appDBContext = appDBContext;
            repositorioCargas = RepositorioCargas;
            this.repositorioExcepciones = repositorioExcepciones; // Asignado
            connectionString = configuration.GetConnectionString("CadenaSQL");
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
    


    public async Task<IActionResult> Matriz()
        {
            MatrizViewModel modelo = new MatrizViewModel();
            modelo.Sistemas = await repositorioCargas.SistemasMatriz();

            // Asigna el ID de usuario a ViewBag
            ViewBag.IdUsuarioLogueado = HttpContext.Session.GetInt32("IdUsuarioLogueado");

            return View(modelo);
        }

        public async Task<IActionResult> ListaSinPerfil()
        {
            MatrizViewModel modelo = new MatrizViewModel();
            modelo.Sistemas = await repositorioCargas.SistemasMatriz();

            // Asigna el ID de usuario a ViewBag
            ViewBag.IdUsuarioLogueado = HttpContext.Session.GetInt32("IdUsuarioLogueado");

            return View(modelo);
        }


        public async Task<IActionResult> Sistema()
		{
			var modelo = new MatrizViewModel
			{
				Sistemas = await repositorioCargas.ObtenerListaSistemaManual()
			};
			return View(modelo);
		}


        [Route("Archivos/ListaMatrizResumida/{idUsuarioLogueado}")]
        public async Task<IActionResult> ListaMatrizResumida(int idUsuarioLogueado)
        {
            var usuario = await _appDBContext.Usuario
                .Include(u => u.Gerencia) // Incluir la gerencia asociada
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioLogueado);

            // Imprimir información del usuario encontrado
            if (usuario != null)
            {
                Console.WriteLine($"Usuario encontrado: ID={usuario.IdUsuario}, Rol={usuario.idRol}, ID_Gerencia={usuario.ID_gerencia}");
            }
            else
            {
                Console.WriteLine("No se encontró el usuario.");
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
            }


            // Verificar si el usuario tiene gerencia asignada
            bool tieneGerencia = usuario.ID_gerencia != null;
            ViewBag.TieneGerencia = tieneGerencia; // Indicar si el usuario tiene gerencia

            if (!tieneGerencia)
            {
                // Si el usuario no tiene gerencia, obtener todas las gerencias
                var resumenMatrizTodasGerencias = await repositorioCargas.ObtenerResumenMatrizTodasGerencias();

                // Imprimir la cantidad de resultados obtenidos
                Console.WriteLine($"Cantidad de gerencias obtenidas: {resumenMatrizTodasGerencias.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ResumenMatrizPerfil = resumenMatrizTodasGerencias
                };

                ViewBag.ID = idUsuarioLogueado;
                return View(modelo);
            }
            else
            {
                // Obtener la lista de matriz filtrada por la gerencia del usuario
                var resumenMatrizPorGerencia = await repositorioCargas.ObtenerResumenMatrizPorGerencia(idUsuarioLogueado);

                // Imprimir la cantidad de resultados filtrados
                Console.WriteLine($"Cantidad de resultados filtrados: {resumenMatrizPorGerencia.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ResumenMatrizPerfil = resumenMatrizPorGerencia
                };

                ViewBag.ID = idUsuarioLogueado;
                return View(modelo);
            }
        }

        [HttpGet("Archivos/VerDetalle/{id}")]
        public async Task<IActionResult> VerDetalle(int id)
        {
            // Validar que tengamos un usuario logueado
            var idUsuarioLogueado = ObtenerIdUsuarioDesdeSesion();
            if (!idUsuarioLogueado.HasValue)
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
            }

            // Obtener el usuario logueado y su gerencia
            var usuario = await _appDBContext.Usuario
                .Include(u => u.Gerencia)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioLogueado);

            if (usuario == null)
            {
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
            }

            bool tieneGerencia = usuario.ID_gerencia != null;
            ViewBag.TieneGerencia = tieneGerencia;
            ViewBag.ID = idUsuarioLogueado;

            // Obtener el detalle de la firma específica
            var firmaDetalle = tieneGerencia
                ? await repositorioCargas.FirmasMatrizPorGerenciaYFirma(idUsuarioLogueado.Value, id)
                : await repositorioCargas.FirmasMatrizTodasGerenciasPorFirma(id);

            var modelo = new ListaMatrizViewModel
            {
                FirmasMatriz = firmaDetalle
            };

            return View("ListaFirmasMatriz", modelo);
        }




        [Route("Archivos/ListaMatriz/{idUsuarioLogueado}/{idSistema}")]
        public async Task<IActionResult> ListaMatriz(int idUsuarioLogueado, int idSistema)
        {
            var usuario = await _appDBContext.Usuario
                .Include(u => u.Gerencia) // Incluir la gerencia asociada
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioLogueado);

            // Imprimir información del usuario encontrado
            if (usuario != null)
            {
                Console.WriteLine($"Usuario encontrado: ID={usuario.IdUsuario}, Rol={usuario.idRol}, ID_Gerencia={usuario.ID_gerencia}");
            }
            else
            {
                Console.WriteLine("No se encontró el usuario.");
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
            }

			// Verificar si el usuario es responsable de la firma
			bool esResponsableFirma = usuario.ResponsableFirma ?? false; // Si es null, toma false
			ViewBag.EsResponsableFirma = esResponsableFirma;

			// Obtener la lista de matriz usando el IdSistema
			var listaMatrizPerfil = await repositorioCargas.ObtenerListaDataMatriz(idSistema);

            // Verificar si el usuario tiene gerencia asignada
            bool tieneGerencia = usuario.ID_gerencia != null;
            ViewBag.TieneGerencia = tieneGerencia; // Indicar si el usuario tiene gerencia

            if (!tieneGerencia)
            {
                // Si el usuario no tiene gerencia, obtener todas las gerencias
                var listaMatrizPerfilTodasGerencias = await repositorioCargas.ObtenerListaDataMatrizTodasGerencias();

                // Imprimir la cantidad de resultados obtenidos
                Console.WriteLine($"Cantidad de gerencias obtenidas: {listaMatrizPerfilTodasGerencias.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ListaMatrizPerfil = listaMatrizPerfilTodasGerencias
                };

                ViewBag.ID = idUsuarioLogueado;
                ViewBag.IdSistema = idSistema;
                return View(modelo);
            }
            else
            {
                // Obtener la lista de matriz filtrada por la gerencia del usuario
                var listaMatrizPerfilPorGerencia = await repositorioCargas.ObtenerListaDataMatrizPorGerencia(idUsuarioLogueado);

                // Imprimir la cantidad de resultados filtrados
                Console.WriteLine($"Cantidad de resultados filtrados: {listaMatrizPerfilPorGerencia.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ListaMatrizPerfil = listaMatrizPerfilPorGerencia
                };

                ViewBag.ID = idUsuarioLogueado;
                ViewBag.IdSistema = idSistema;
                return View(modelo);
            }
        }

        [Route("Archivos/UsuariosSinPerfil/{idUsuarioLogueado}/{idSistema}")]
        public async Task<IActionResult> UsuariosSinPerfil(int idUsuarioLogueado, int idSistema)
        {
            var usuario = await _appDBContext.Usuario
                .Include(u => u.Gerencia) // Incluir la gerencia asociada
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioLogueado);

            // Imprimir información del usuario encontrado
            if (usuario != null)
            {
                Console.WriteLine($"Usuario encontrado: ID={usuario.IdUsuario}, Rol={usuario.idRol}, ID_Gerencia={usuario.ID_gerencia}");
            }
            else
            {
                Console.WriteLine("No se encontró el usuario.");
                return BadRequest(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
            }

            // Obtener la lista de matriz usando el IdSistema
            var listaMatrizPerfil = await repositorioCargas.ObtenerListaDataMatriz(idSistema);

            // Verificar si el usuario tiene gerencia asignada
            bool tieneGerencia = usuario.ID_gerencia != null;
            ViewBag.TieneGerencia = tieneGerencia; // Indicar si el usuario tiene gerencia

            if (!tieneGerencia)
            {
                // Si el usuario no tiene gerencia, obtener todas las gerencias
                var listaMatrizPerfilTodasGerencias = await repositorioCargas.ObtenerListaSinPerfilTodasGerencias();

                // Imprimir la cantidad de resultados obtenidos
                Console.WriteLine($"Cantidad de gerencias obtenidas: {listaMatrizPerfilTodasGerencias.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ListaMatrizPerfil = listaMatrizPerfilTodasGerencias
                };

                ViewBag.ID = idUsuarioLogueado;
                ViewBag.IdSistema = idSistema;
                return View(modelo);
            }
            else
            {
                // Obtener la lista de matriz filtrada por la gerencia del usuario
                var listaMatrizPerfilPorGerencia = await repositorioCargas.ObtenerListaSinPerfilPorGerencia(idUsuarioLogueado);

                // Imprimir la cantidad de resultados filtrados
                Console.WriteLine($"Cantidad de resultados filtrados: {listaMatrizPerfilPorGerencia.Count()}");

                var modelo = new ListaMatrizViewModel
                {
                    ListaMatrizPerfil = listaMatrizPerfilPorGerencia
                };

                ViewBag.ID = idUsuarioLogueado;
                ViewBag.IdSistema = idSistema;
                return View(modelo);
            }
        }



        [HttpPost]
        public async Task<JsonResult> CrearFirmaConDetalles(int idUsuarioLogueado, int idSistema, string comentario)
        {
            try
            {
                _logger.LogInformation($"Iniciando CrearFirmaConDetalles para usuario {idUsuarioLogueado}");

                // Obtener usuario
                var usuario = await _appDBContext.Usuario
                    .Include(u => u.Gerencia)
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioLogueado);

                if (usuario == null)
                {
                    _logger.LogWarning($"Usuario no encontrado: {idUsuarioLogueado}");
                    return Json(new { RESPUESTA = false, MENSAJE = "Usuario no encontrado." });
                }

                // Obtener los datos de la matriz
                IEnumerable<dynamic> datosMatriz;
                if (usuario.ID_gerencia == null && usuario.idRol == 2)
                {
                    datosMatriz = await repositorioCargas.ObtenerListaDataMatrizTodasGerencias();
                }
                else
                {
                    datosMatriz = await repositorioCargas.ObtenerListaDataMatrizPorGerencia(idUsuarioLogueado);
                }

                // Filtrar registros con EstadoMatrizPerfil "Correcto" o "Autorizado"
                var datosFiltrados = datosMatriz
                    .Where(dm => dm.EstadoMatrizPerfil == "Correcto" || dm.EstadoMatrizPerfil == "Autorizado")
                    .ToList();

                if (!datosFiltrados.Any())
                {
                    _logger.LogWarning($"No hay datos 'Correcto' o 'Autorizado' para firmar para el usuario {idUsuarioLogueado}");
                    return Json(new { RESPUESTA = false, MENSAJE = "No hay datos 'Correcto' o 'Autorizado' para firmar." });
                }

                // Crear la firma (sin restricciones de tiempo)
                var firma = new ImFirma
                {
                    CodUsuarioResponsable = idUsuarioLogueado,
                    FechaFirma = DateTime.Now,
                    CodGerencia = usuario.ID_gerencia,
                    Comentario = comentario,
                    FechaCarga = datosFiltrados.First().feccarga
                };

                // Crear la lista de detalles de firma solo para registros con "Correcto" o "Autorizado"
                var detallesFirma = datosFiltrados.Select(dm => new ImDetalleFirma
                {
                    pais = dm.pais,
                    sistema = dm.sistema,
                    idPaisNegocioSistema = dm.idPaisNegocioSistema,
                    rutdni = dm.rutdni,
                    dv = dm.dv,
                    nombreusuario = dm.nombreusuario,
                    userid = dm.userid,
                    cargospr = dm.cargospr,
                    perfil = dm.perfil,
                    codccosto = dm.codccosto,
                    codccostospr = dm.codccostospr,
                    Nomccosto = dm.Nomccosto,
                    cargomatriz = dm.cargomatriz,
                    perfilmatriz = dm.perfilmatriz,
                    feccarga = dm.feccarga,
                    cta_duplicada = dm.cta_duplicada,
                    Evidencia = dm.Evidencia
                }).ToList();

                // Insertar firma y detalles sin restricciones de tiempo
                var seInserto = await repositorioCargas.InsertarFirmaYDetalles(firma, detallesFirma);
                _logger.LogInformation($"InsertarFirmaYDetalles para usuario {idUsuarioLogueado}: {seInserto}");

                if (seInserto)
                {
                    return Json(new { RESPUESTA = true, MENSAJE = "Firma creada exitosamente." });
                }
                else
                {
                    return Json(new { RESPUESTA = false, MENSAJE = "No se pudo crear la firma. Posiblemente ya existe." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en CrearFirmaConDetalles para usuario {idUsuarioLogueado}");
                return Json(new { RESPUESTA = false, MENSAJE = $"Error al crear la firma: {ex.Message}" });
            }
        }





        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ListaMatrizJson(int Id)
        {
            try
            {
                var sistemaInfo = await repositorioCargas.ObtenerSistemasMatrizPorId(Id);
                var json = await repositorioCargas.ObtenerListaDataMatriz(Id);

                // Verifica si json es nulo o vacío
                //if (json == null || !json.Any())
                //{
                //    return BadRequest(new { RESPUESTA = false, TIPO = 2, MENSAJE = "No se encontraron datos." });
                //}

                var objeto = new { RESPUESTA = true, DATA = json, TIPO = 1, TITULO = sistemaInfo };
                return Ok(objeto);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { RESPUESTA = false, TIPO = 3, MENSAJE = "Error al procesar la solicitud." });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ListaMatrizJsonResp(int Id)
        {
            try
            {
                var sistemaInfo = await repositorioCargas.ObtenerSistemasMatrizPorId(Id);
                var json = await repositorioCargas.ObtenerListaDataMatrizRespaldo(Id);

                //// Verifica si json es nulo o vacío
                //if (json == null || !json.Any())
                //{
                //    return Json(new { RESPUESTA = false, TIPO = 2, MENSAJE = "No se encontraron datos." });
                //}

                var objeto = new { RESPUESTA = true, DATA = json, TIPO = 1, TITULO = sistemaInfo };
                return Ok(objeto);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { RESPUESTA = false, TIPO = 3, MENSAJE = "Error al procesar la solicitud." });
            }
        }



        [Route("Archivos/SubirArchivo/{IdSistema}")]
        public async Task<ActionResult> SubirArchivo(int IdSistema)
        {
            ViewBag.e = TempData["RespuestaE"];
            var modelo = await repositorioCargas.ObtenerSistemasMatrizPorId(IdSistema);
            return View(modelo);
        }


        [HttpPost]
        public async Task<IActionResult> CargarArchivo(IFormFile FileUpload, int Id)
        {
            var modelo = await repositorioCargas.ObtenerListaDataMatriz(Id);

            if (FileUpload != null && FileUpload.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        await FileUpload.CopyToAsync(stream);

                        IExcelDataReader reader = null;
                        if (FileUpload.FileName.EndsWith(".xls"))
                        {
                            reader = ExcelReaderFactory.CreateBinaryReader(stream);
                        }
                        else if (FileUpload.FileName.EndsWith(".xlsx"))
                        {
                            reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        }

                        if (reader != null)
                        {
                            var lista = new List<cl_sodimac_matriz_perfil>();
                            var result = reader.AsDataSet();
                            reader.Close();

                            var table = result.Tables[0];

                            if (table.Rows.Count > 0)
                            {
                                for (int i = 1; i < table.Rows.Count; i++)
                                {
                                    var fila = new cl_sodimac_matriz_perfil
                                    {
                                        modulo = table.Rows[i][0].ToString().Trim(),
                                        perfil = table.Rows[i][1].ToString().Trim(),
                                        codPerfil = table.Rows[i][2].ToString().Trim(),
                                        Fcarga = !string.IsNullOrEmpty(table.Rows[i][3].ToString().Trim()) ? Convert.ToDateTime(table.Rows[i][3].ToString().Trim()) : (DateTime?)null,
                                        responsable = table.Rows[i][4].ToString().Trim(),
                                        cargoActivo = table.Rows[i][5].ToString().Trim(),
                                        cargo = table.Rows[i][6].ToString().Trim(),
                                        idNivelCargo = !string.IsNullOrEmpty(table.Rows[i][7].ToString().Trim()) ? Convert.ToInt32(table.Rows[i][7].ToString().Trim()) : (int?)null,
                                        idPaisNegocioSistema = !string.IsNullOrEmpty(table.Rows[i][8].ToString().Trim()) ? Convert.ToInt32(table.Rows[i][8].ToString().Trim()) : (int?)null,
                                        idUsuario = !string.IsNullOrEmpty(table.Rows[i][9].ToString().Trim()) ? Convert.ToInt32(table.Rows[i][9].ToString().Trim()) : (int?)null,
                                        estado_matriz = table.Rows[i][10].ToString().Trim(),
                                        matriz = table.Rows[i][11].ToString().Trim(),
                                        usocargo = table.Rows[i][12].ToString().Trim(),
                                        tipocarga = table.Rows[i][13].ToString().Trim(),
                                        nivelcargo = table.Rows[i][14].ToString().Trim()
                                    };

                                    lista.Add(fila);
                                }
                            }

                            await repositorioCargas.RegistrarDatosMatrizPerfil(modelo, Id, lista);
                            TempData["RespuestaE"] = 3; // REGISTRO CORRECTO
                            return RedirectToAction("SubirArchivo", "Archivos", new { Id });
                        }
                    }

                    TempData["RespuestaE"] = 1; // Error en el formato de archivo
                    return RedirectToAction("SubirArchivo", "Archivos", new { Id });
                }
                catch (Exception)
                {
                    TempData["RespuestaE"] = 1; // Error en la carga del archivo
                    return RedirectToAction("SubirArchivo", "Archivos", new { Id });
                }
            }

            TempData["RespuestaE"] = 1; // Archivo no proporcionado
            return RedirectToAction("SubirArchivo", "Archivos", new { Id });
        }





        [Route("Archivos/SubirArchivoManual/{Id}")]
        public async Task<ActionResult> SubirArchivoManual(int Id)
        {
            ViewBag.e = TempData["RespuestaE"];

            var modelo = await repositorioCargas.ObtenerSistemaManualPorId(Id);
            return View(modelo);

        }


        [HttpPost]
        public async Task<ActionResult> CargarArchivoManual(List<IFormFile> FileUpload, int Id, string NombreSistema)
        {
            if (HttpContext.Session.GetString("User") != null)  // Cambiado a HttpContext.Session.GetString("User")
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string UrlSubida = Path.Combine(webRootPath, "ArchivosManuales", "im", NombreSistema);

                var modelo = await repositorioCargas.ObtenerListaDataMatriz(Id);
                int UserId = Convert.ToInt32(HttpContext.Session.GetString("User"));

                if (!Directory.Exists(UrlSubida))
                {
                    Directory.CreateDirectory(UrlSubida);
                }

                foreach (var item in FileUpload)
                {
                    if (item != null && item.Length > 0)
                    {
                        string filePath = Path.Combine(UrlSubida, item.FileName);

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await item.CopyToAsync(stream);
                        }
                    }
                }

                TempData["RespuestaE"] = 3; // REGISTRO CORRECTO
                return RedirectToAction("SubirArchivoManual", "Archivos", new { @Id = Id });
            }
            else
            {
                return RedirectToAction("Login", "Acceso");
            }
        }

        //FILTRO FIRMAS

        private int? ObtenerIdUsuarioDesdeSesion()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuarioLogueado");
            return idUsuario;
        }


        [HttpGet("FiltroFirmas")]
        public async Task<IActionResult> FiltroFirmas(string fechaSeleccionada = null)
        {
            var idUsuarioLogueado = ObtenerIdUsuarioDesdeSesion();
            ViewBag.ID = idUsuarioLogueado;
            ViewBag.Firmas = null;
            ViewBag.FechaSeleccionada = fechaSeleccionada;
            ViewBag.Excepciones = null; // Se elimina la carga de excepciones

            if (idUsuarioLogueado == null)
            {
                Console.WriteLine("⚠ Usuario no logueado. No se detectó ID en sesión.");
                return View(new FiltroFirmasViewModel());
            }

            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var usuario = await db.QueryFirstOrDefaultAsync<Usuario>(
                    "SELECT ID_gerencia, ID_Subgerencia FROM usuario WHERE IdUsuario = @idUsuarioLogueado",
                    new { idUsuarioLogueado });

                List<string> fechasDisponibles = new List<string>();
                List<ImFirma> firmasFiltradas = new List<ImFirma>();

                // 🔹 Obtener fechas disponibles
                var queryFechas = usuario != null && usuario.ID_gerencia != null
                    ? @"
                SELECT DISTINCT 
                    FORMAT(fechaFirma, 'MMMM yyyy', 'es-ES') AS FechaTexto,
                    YEAR(fechaFirma) AS Anio,
                    MONTH(fechaFirma) AS Mes
                FROM im_firma
                WHERE codUsuarioResponsable = @idUsuarioLogueado
                ORDER BY Anio DESC, Mes DESC"
                    : @"
                SELECT DISTINCT 
                    FORMAT(fechaFirma, 'MMMM yyyy', 'es-ES') AS FechaTexto,
                    YEAR(fechaFirma) AS Anio,
                    MONTH(fechaFirma) AS Mes
                FROM im_firma
                ORDER BY Anio DESC, Mes DESC";

                var fechas = await db.QueryAsync<(string FechaTexto, int Anio, int Mes)>(queryFechas, usuario != null && usuario.ID_gerencia != null
                    ? new { idUsuarioLogueado }
                    : null);

                fechasDisponibles = fechas.Select(f => $"{f.FechaTexto}|{f.Anio}-{f.Mes}").ToList();
                ViewBag.FechasDisponibles = fechasDisponibles;

                // 🔹 Si se selecciona una fecha, obtener firmas
                if (!string.IsNullOrEmpty(fechaSeleccionada))
                {
                    var partes = fechaSeleccionada.Split('-');
                    int anio = int.Parse(partes[0]);
                    int mes = int.Parse(partes[1]);

                    string queryFirmas = usuario != null && usuario.ID_gerencia != null
                        ? @"
                    SELECT f.*, 
                           u.nombre AS NombreUsuario,
                           g.Nom_Gerencia,
                           u.firma AS FirmaBase64
                    FROM im_firma f
                    INNER JOIN usuario u ON f.codUsuarioResponsable = u.idUsuario
                    LEFT JOIN im_gerencia g ON f.codGerencia = g.ID_gerencia
                    WHERE f.codUsuarioResponsable = @idUsuarioLogueado
                    AND YEAR(f.fechaFirma) = @anio
                    AND MONTH(f.fechaFirma) = @mes
                    ORDER BY f.fechaFirma DESC"
                        : @"
                    SELECT f.*, 
                           u.nombre AS NombreUsuario,
                           g.Nom_Gerencia,
                           u.firma AS FirmaBase64
                    FROM im_firma f
                    INNER JOIN usuario u ON f.codUsuarioResponsable = u.idUsuario
                    LEFT JOIN im_gerencia g ON f.codGerencia = g.ID_gerencia
                    WHERE YEAR(f.fechaFirma) = @anio
                    AND MONTH(f.fechaFirma) = @mes
                    ORDER BY f.fechaFirma DESC";

                    firmasFiltradas = (await db.QueryAsync<ImFirma>(queryFirmas, usuario != null && usuario.ID_gerencia != null
                        ? new { idUsuarioLogueado, anio, mes }
                        : new { anio, mes })).ToList();

                    ViewBag.Firmas = firmasFiltradas;
                }
            }

            return View(new FiltroFirmasViewModel());
        }



        [HttpGet("ObtenerExcepcionesPorFirma")]
        public async Task<IActionResult> ObtenerExcepcionesPorFirma(int idFirma)
        {
            if (idFirma <= 0)
            {
                return BadRequest("ID de Firma inválido.");
            }

            List<Comentarios> excepciones = await repositorioExcepciones.ObtenerExcepcionesPorFirma(idFirma);

            if (excepciones == null || !excepciones.Any())
            {
                return NotFound("No se encontraron excepciones para la firma proporcionada.");
            }

            return Ok(excepciones);
        }



        [HttpGet("FiltrarFirmas")]
        public IActionResult FiltrarFirmas(int idUsuarioLogueado, string fechaSeleccionada)
        {
            // Extraer año y mes del formato "YYYY-MM"
            var partes = fechaSeleccionada.Split('-');
            int anio = int.Parse(partes[0]);
            int mes = int.Parse(partes[1]);

            // Redirigir con los parámetros correctos
            return RedirectToAction("ListaFirmasMatriz", new { idUsuarioLogueado, mes, anio });
        }




    }
}