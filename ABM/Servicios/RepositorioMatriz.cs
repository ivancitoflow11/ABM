using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; 
using Microsoft.AspNetCore.Hosting;

namespace ABM.Servicios
{
    public interface IRepositorioMatriz
    {
        Task<IEnumerable<MatrizActivo>> ObtenerListaDataMatrizTodo(int idSistema, string nombrePais, int idNegocio);
        Task<IEnumerable<MatrizDisponible>> ObtenerMatricesDisponibles(int idPais, int idNegocio);
        Task<bool> EsResponsableFirma(int idUsuario);
        Task<Firma> ObtenerFirmaExistente(int idUsuario, int idPais, int idNegocio, int idSistema);
        Task<bool> CrearFirmaYDetallesAsync(int idUsuario, int idPais, int idNegocio, int idSistema, string comentario);
        Task<bool> EliminarFirmaAsync(int idFirma);
        string GetConnectionString();
        Task<IEnumerable<Firma>> ObtenerTodasLasFirmas();
        Task<FirmaReporteViewModel> ObtenerDatosParaReporteFirma(int idFirma);
    }

    public class RepositorioMatriz : IRepositorioMatriz
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RepositorioMatriz> _logger; // <-- CAMBIO 1: AÑADIR CAMPO PARA EL LOGGER
        private readonly IWebHostEnvironment _hostingEnvironment;

        public RepositorioMatriz(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<RepositorioMatriz> logger, IWebHostEnvironment hostingEnvironment)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger; // Asignar el logger
            _hostingEnvironment = hostingEnvironment;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        public async Task<FirmaReporteViewModel> ObtenerDatosParaReporteFirma(int idFirma)
        {
            using var connection = Connection;
            var firmaInfo = await connection.QuerySingleOrDefaultAsync<Firma>(
                @"SELECT 
            f.idFirma, f.fechaFirma, f.comentario, f.codUsuarioResponsable,
            u.nombre + ' ' + u.apellidos AS NombreUsuarioResponsable,
            p.pais AS NombrePais, n.negocio AS NombreNegocio, s.sistema AS NombreSistema
          FROM ftc_firma f
          JOIN ftc_usuario u ON f.codUsuarioResponsable = u.idUsuario
          LEFT JOIN ftc_pais p ON f.idPais = p.idPais
          LEFT JOIN ftc_negocio n ON f.idNegocio = n.idNegocio
          LEFT JOIN ftc_sistema s ON f.idSistema = s.idSistema
          WHERE f.idFirma = @idFirma", new { idFirma });

            if (firmaInfo == null) return null;

            // --- INICIO DE LA LÓGICA DE LA IMAGEN ---

            // 1. Obtenemos la RUTA del archivo de la firma desde la BD
            var rutaRelativaFirma = await connection.QuerySingleOrDefaultAsync<string>(
                "SELECT firma FROM ftc_usuario WHERE idUsuario = @codUsuarioResponsable",
                new { firmaInfo.codUsuarioResponsable });

            string firmaEnBase64 = null;
            if (!string.IsNullOrEmpty(rutaRelativaFirma))
            {
                try
                {
                    // 2. Combinamos la ruta web con la ruta del servidor para obtener la ruta física completa
                    // Nota: Se elimina la barra inicial si existe para que Path.Combine funcione bien.
                    var rutaFisica = System.IO.Path.Combine(_hostingEnvironment.WebRootPath, rutaRelativaFirma.TrimStart('/'));

                    if (System.IO.File.Exists(rutaFisica))
                    {
                        // 3. Leemos el archivo y lo convertimos a Base64
                        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(rutaFisica);
                        string base64String = Convert.ToBase64String(imageBytes);
                        // 4. Formateamos la cadena para que el HTML la entienda como una imagen
                        firmaEnBase64 = $"data:image/png;base64,{base64String}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar el archivo de firma para el usuario {codUsuarioResponsable}", firmaInfo.codUsuarioResponsable);
                }
            }
            // --- FIN DE LA LÓGICA DE LA IMAGEN ---

            var detalles = await connection.QueryAsync<DetalleFirma>(
                @"SELECT 
          df.*,
          n.negocio 
      FROM 
          ftc_detalle_firma df
      LEFT JOIN 
          ftc_negocio n ON df.idNegocio = n.idNegocio
      WHERE 
          df.codFirma = @idFirma 
      ORDER BY 
          df.nombreusuario", new { idFirma });

            return new FirmaReporteViewModel
            {
                FirmaInfo = firmaInfo,
                FirmaUsuarioBase64 = firmaEnBase64, // imagen ya convertida
                DetallesFirma = detalles
            };
        }
        public async Task<IEnumerable<Firma>> ObtenerTodasLasFirmas()
        {
            using var connection = Connection;
            const string query = @"
                SELECT 
                    f.idFirma, f.fechaFirma, f.comentario,
                    u.nombre + ' ' + u.apellidos AS NombreUsuarioResponsable,
                    p.pais AS NombrePais,
                    n.negocio AS NombreNegocio,
                    s.sistema AS NombreSistema
                FROM 
                    ftc_firma f
                JOIN 
                    ftc_usuario u ON f.codUsuarioResponsable = u.idUsuario
                LEFT JOIN 
                    ftc_pais p ON f.idPais = p.idPais
                LEFT JOIN 
                    ftc_negocio n ON f.idNegocio = n.idNegocio
                LEFT JOIN 
                    ftc_sistema s ON f.idSistema = s.idSistema
                ORDER BY 
                    f.fechaFirma DESC;";

            return await connection.QueryAsync<Firma>(query);
        }
        private IDbConnection Connection => new SqlConnection(_connectionString);

        private int ObtenerIdUsuarioActual()
        {
            var idClaim = _httpContextAccessor.HttpContext.User.FindFirst("idUsuario")?.Value ?? _httpContextAccessor.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim ?? "0");
        }

        public async Task<IEnumerable<MatrizDisponible>> ObtenerMatricesDisponibles(int idPais, int idNegocio)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                const string query = @"
        SELECT DISTINCT
            p.pais,
            s.sistema,
            s.idSistema,
            pns.idNegocio      
        FROM
            dbo.ftc_pais_negocio_sistema pns
        JOIN
            dbo.ftc_pais p ON pns.idPais = p.idPais
        JOIN
            dbo.ftc_sistema s ON pns.idSistema = s.idSistema
        JOIN
            dbo.ftc_agrupa_activos aa ON pns.idPaisNegocioSistema = aa.idPaisNegocioSistema
        JOIN -- <-- 1. NUEVO JOIN A LA TABLA pnsjt
            dbo.ftc_pnsjt jt ON pns.idPaisNegocioSistema = jt.idPaisNegocioSistema
        WHERE
            pns.idPais = @IdPais
            AND pns.idNegocio = @IdNegocio
            AND pns.estado = 1
            AND aa.perfil IS NOT NULL
            AND jt.infomatrizperfil = 'SI' -- <-- 2. NUEVO FILTRO
        ORDER BY
            p.pais, s.sistema;
    ";
                return await db.QueryAsync<MatrizDisponible>(query, new { IdPais = idPais, IdNegocio = idNegocio });
            }
        }

        public async Task<IEnumerable<MatrizActivo>> ObtenerListaDataMatrizTodo(int idSistema, string nombrePais, int idNegocio)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                const string query = @"
                    SELECT 
                        aa.*,
                        p.pais,
                        s.sistema,
                        g.Nom_Gerencia,
                        sg.Nom_Subgerencia,
                        n.negocio
                    FROM dbo.ftc_agrupa_activos aa
                    JOIN dbo.ftc_pais_negocio_sistema pns ON aa.idPaisNegocioSistema = pns.idPaisNegocioSistema
                    JOIN dbo.ftc_sistema s ON pns.idSistema = s.idSistema
                    JOIN dbo.ftc_pais p ON pns.idPais = p.idPais
                    JOIN dbo.ftc_negocio n ON pns.idNegocio = n.idNegocio
                    LEFT JOIN dbo.ftc_Subgerencias sg ON aa.Nomccostospr = sg.Nom_Subgerencia
                    LEFT JOIN dbo.ftc_gerencia g ON sg.COD_Gerencia = g.ID_gerencia
                    WHERE s.idSistema = @IdSistema 
                        AND p.pais = @NombrePais
                        AND pns.idNegocio = @IdNegocio;
                ";
                return await db.QueryAsync<MatrizActivo>(query, new { IdSistema = idSistema, NombrePais = nombrePais, IdNegocio = idNegocio });
            }
        }

        public async Task<bool> EsResponsableFirma(int idUsuario)
        {
            using var connection = Connection;
            return await connection.QuerySingleOrDefaultAsync<bool>(
                @"SELECT CAST(CASE WHEN ResponsableFirma = 1 THEN 1 ELSE 0 END AS BIT)
                  FROM ftc_usuario WHERE idUsuario = @idUsuario;",
                new { idUsuario });
        }

        public async Task<Firma> ObtenerFirmaExistente(int idUsuario, int idPais, int idNegocio, int idSistema)
        {
            using var connection = Connection;
            return await connection.QuerySingleOrDefaultAsync<Firma>(
                @"SELECT TOP 1 * FROM ftc_firma
                  WHERE codUsuarioResponsable = @idUsuario
                  AND idPais = @idPais
                  AND idNegocio = @idNegocio
                  AND idSistema = @idSistema
                  AND MONTH(fechaFirma) = MONTH(GETDATE())
                  AND YEAR(fechaFirma) = YEAR(GETDATE());",
                new { idUsuario, idPais, idNegocio, idSistema });
        }

        public async Task<bool> CrearFirmaYDetallesAsync(int idUsuario, int idPais, int idNegocio, int idSistema, string comentario)
        {
            var firmaExistente = await ObtenerFirmaExistente(idUsuario, idPais, idNegocio, idSistema);
            if (firmaExistente != null)
            {
                return false;
            }

            using var connection = Connection;
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var usuario = await connection.QuerySingleOrDefaultAsync<Usuario>(
                    "SELECT ID_gerencia FROM ftc_usuario WHERE idUsuario = @idUsuario",
                    new { idUsuario }, transaction);

                var sqlFirma = @"
            INSERT INTO ftc_firma (codUsuarioResponsable, fechaFirma, comentario, codGerencia, idPais, idNegocio, idSistema)
            VALUES (@codUsuarioResponsable, GETDATE(), @comentario, @codGerencia, @idPais, @idNegocio, @idSistema);
            SELECT SCOPE_IDENTITY();";

                var nuevaFirmaId = await connection.QuerySingleAsync<int>(sqlFirma, new
                {
                    codUsuarioResponsable = idUsuario,
                    comentario = comentario,
                    codGerencia = usuario?.ID_gerencia,
                    idPais = idPais,
                    idNegocio = idNegocio,
                    idSistema = idSistema
                }, transaction);

                var nombrePais = await connection.QuerySingleAsync<string>("SELECT pais FROM ftc_pais WHERE idPais = @IdPais", new { IdPais = idPais }, transaction);
                var datosMatriz = await ObtenerListaDataMatrizTodo(idSistema, nombrePais, idNegocio);

                var sqlDetalle = @"
            INSERT INTO ftc_detalle_firma (codFirma, pais, sistema, idPaisNegocioSistema, rutdni, dv, nombreusuario, userid, cargospr, perfil, codccosto, codccostospr, Nomccosto, cargomatriz, perfilmatriz, feccarga, cta_duplicada, idNegocio)
            VALUES (@codFirma, @pais, @sistema, @idPaisNegocioSistema, @rutdni, @dv, @nombreusuario, @userid, @cargospr, @perfil, @codccosto, @codccostospr, @Nomccosto, @cargomatriz, @perfilmatriz, @feccarga, @cta_duplicada, @idNegocio);";

                foreach (var fila in datosMatriz)
                {
                    var parametrosDetalle = new
                    {
                        codFirma = nuevaFirmaId,
                        pais = fila.Pais,
                        sistema = fila.Sistema,
                        idPaisNegocioSistema = fila.IdPaisNegocioSistema,
                        rutdni = fila.Rutdni,
                        dv = fila.Dv,
                        nombreusuario = fila.NombreUsuario,
                        userid = fila.UserId,
                        cargospr = fila.CargoSpr,
                        perfil = fila.Perfil,
                        codccosto = fila.CodCcosto,
                        codccostospr = fila.CodCcostoSpr,
                        Nomccosto = fila.NomCcosto,
                        cargomatriz = fila.CargoMatriz,
                        perfilmatriz = fila.PerfilMatriz,
                        feccarga = fila.FecCarga,
                        cta_duplicada = fila.Cta_Duplicada,
                        idNegocio = idNegocio
                    };
                    await connection.ExecuteAsync(sqlDetalle, (object)parametrosDetalle, transaction);
                }

                transaction.Commit();
                return true;
            }
            // CAMBIO 3: ACTUALIZAR BLOQUE CATCH PARA MOSTRAR EL ERROR
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la firma y sus detalles en la base de datos.");
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> EliminarFirmaAsync(int idFirma)
        {
            using var connection = Connection;
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync("DELETE FROM ftc_detalle_firma WHERE codFirma = @idFirma", new { idFirma }, transaction);
                await connection.ExecuteAsync("DELETE FROM ftc_firma WHERE idFirma = @idFirma", new { idFirma }, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la firma con ID {IdFirma}", idFirma);
                transaction.Rollback();
                return false;
            }
        }
    }
}