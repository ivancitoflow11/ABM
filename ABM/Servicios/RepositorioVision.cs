using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
    public interface IRepositorioVision
    {
        Task<IEnumerable<PaisViewModel>> ObtenerListaPaisesPorNegocio(int idNegocio);
        Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion(int idPais, int idNegocio);
        Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaEvolucionParametro(int sistema, int idPais, int idNegocio);
        Task<IEnumerable<RiesgoPaisViewModel>> ObtenerListaRiesgoPais(int idPais, int idNegocio);
        Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerListaRiesgoSistema(int idPais, int idNegocio);
        Task<IEnumerable<RiesgoPaisViewModel>> ListaRiesgoPorNegocio(int idNegocio);
        Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerRiesgoPerfil(int idPais, int idNegocio);
        Task<IEnumerable<Abm_Sistema>> ListaDeSistemas(int idPais, int idNegocio);
    }

    public class RepositorioVision : IRepositorioVision
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioVision(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<IEnumerable<PaisViewModel>> ObtenerListaPaisesPorNegocio(int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            // CAMBIO: Directo a ftc_pns_2. Eliminado el UNION ALL.
            return await db.QueryAsync<PaisViewModel>(@"
                SELECT DISTINCT
                    ISNULL(idPais, 0)        AS IdPais,  
                    ISNULL(pais, 'SIN PAIS') AS NombrePais  
                FROM dbo.ftc_pns_2
                WHERE idNegocio = @idNegocio
                ORDER BY ISNULL(pais, 'SIN PAIS');
            ", new { idNegocio });
        }

        public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerListaRiesgoSistema(int idPais, int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
            return await db.QueryAsync<RiesgoSistemaViewModel>(@"
            WITH UltimaCarga AS (
                SELECT TOP 1 feccarga
                FROM dbo.ftc_gestion_diaria
                ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
            ),
            ActivosData AS (
                SELECT
                    ISNULL(pns.sistema,'SIN SISTEMA')   AS Sistema,
                    SUM(gd.cnt_activos)                 AS Activos,
                    SUM(gd.cnt_finiquitados)            AS Finiquitados,
                    SUM(gd.cnt_no_encontrados)          AS No_Encontrados
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pns_2 AS pns 
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                WHERE
                    gd.feccarga   = (SELECT feccarga FROM UltimaCarga)
                    AND pns.idPais    = @idPais
                    AND pns.idNegocio = @idNegocio
                GROUP BY ISNULL(pns.sistema,'SIN SISTEMA')  
            ),
            GestionDiariaData AS (
                SELECT DISTINCT
                    ISNULL(pns.sistema,'SIN SISTEMA') AS Sistema
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pns_2 AS pns 
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                WHERE
                    gd.feccarga   = (SELECT feccarga FROM UltimaCarga)
                    AND pns.idPais    = @idPais
                    AND pns.idNegocio = @idNegocio
            )
            SELECT
                A.Sistema,
                A.Activos,
                A.Finiquitados,
                A.No_Encontrados AS NoEncontrados
            FROM ActivosData AS A
            LEFT JOIN GestionDiariaData AS G ON A.Sistema = G.Sistema
            ORDER BY A.Sistema;", new { idPais, idNegocio });
        }

        public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerRiesgoPerfil(int idPais, int idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
                var sql = @"
                SELECT TOP 2   
                    pns.pais,
                    pns.sistema,
                    pns.idSistema,
                    SUM(gd.Riesgo_Alto) AS Riesgo_Alto, 
                    SUM(gd.Riesgo_Medio) AS Riesgo_Medio, 
                    SUM(gd.Riesgo_Bajo) AS Riesgo_Bajo
                FROM dbo.ftc_gestion_diaria gd
                INNER JOIN dbo.ftc_pns_2 pns ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                WHERE pns.idPais       = @idPais
                  AND pns.idNegocio    = @idNegocio
                GROUP BY pns.pais, pns.sistema, pns.idSistema
                ORDER BY MAX(gd.feccarga) DESC;";

                return await dbdapper.QueryAsync<RiesgoSistemaViewModel>(sql, new { idPais, idNegocio });
            }
        }

        public async Task<IEnumerable<RiesgoPaisViewModel>> ObtenerListaRiesgoPais(int idPais, int idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
                var sql = @"
                WITH UltimaCarga AS (
                      SELECT TOP 1 feccarga
                      FROM dbo.ftc_gestion_diaria
                      ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
                  )
                  SELECT
                      UPPER(ISNULL(pns.pais,'SIN PAIS'))     AS pais,
                      SUM(gd.cnt_finiquitados)       AS cnt_finiquitados,
                      SUM(gd.cnt_no_encontrados)     AS cnt_no_encontrados,
                      SUM(gd.cnt_activos)            AS cnt_activos
                  FROM dbo.ftc_gestion_diaria AS gd
                  INNER JOIN dbo.ftc_pns_2 AS pns ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                  WHERE 
                     gd.feccarga       = (SELECT feccarga FROM UltimaCarga)
                     AND pns.idPais    = @idPais
                     AND pns.idNegocio = @idNegocio
                  GROUP BY UPPER(ISNULL(pns.pais,'SIN PAIS')) 
                  ORDER BY UPPER(ISNULL(pns.pais,'SIN PAIS'));";

                return await dbdapper.QueryAsync<RiesgoPaisViewModel>(sql, new { idPais, idNegocio });
            }
        }

        public async Task<IEnumerable<RiesgoPaisViewModel>> ListaRiesgoPorNegocio(int idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
                var sql = @"
                WITH UltimaCarga AS (
                        SELECT TOP 1 feccarga
                        FROM dbo.ftc_gestion_diaria
                        ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
                    )
                    SELECT
                        ISNULL(pns.idPais,0) AS IdPais,              
                        UPPER(ISNULL(pns.pais,'SIN PAIS'))              AS Pais,
                        SUM(gd.cnt_finiquitados)   AS cnt_finiquitados,
                        SUM(gd.cnt_no_encontrados) AS cnt_no_encontrados,
                        SUM(gd.cnt_activos)        AS cnt_activos
                    FROM dbo.ftc_gestion_diaria AS gd 
                    INNER JOIN dbo.ftc_pns_2 AS pns ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                    WHERE 
                        gd.feccarga   = (SELECT feccarga FROM UltimaCarga)
                        AND pns.idNegocio = @idNegocio
                    GROUP BY 
                        ISNULL(pns.idPais,0), 
                        UPPER(ISNULL(pns.pais,'SIN PAIS'))    
                    ORDER BY 
                        UPPER(ISNULL(pns.pais,'SIN PAIS'));";

                return await dbdapper.QueryAsync<RiesgoPaisViewModel>(sql, new { idNegocio });
            }
        }

        public async Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion(int idPais, int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
            return await db.QueryAsync<EvolucionViewModel>(@"
                SELECT TOP 30
                    feccarga,
                    SUM(cnt_finiquitados) AS cantidad_finiquitados
                FROM dbo.ftc_gestion_diaria gd
                INNER JOIN dbo.ftc_pns_2 AS pns ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                WHERE pns.idPais    = @idPais
                  AND pns.idNegocio = @idNegocio
                GROUP BY feccarga
                ORDER BY feccarga DESC;
            ", new { idPais, idNegocio });
        }

        public async Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaEvolucionParametro(int sistema, int idPais, int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            // CAMBIO: Reemplazado subquery UNION por INNER JOIN ftc_pns_2
            return await db.QueryAsync<TendenciaDiariaViewModel>(@"
                SELECT TOP 30
                    feccarga,
                    SUM(cnt_finiquitados) AS cantidad_finiquitados
                FROM dbo.ftc_gestion_diaria gd
                INNER JOIN dbo.ftc_pns_2 AS pns ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                WHERE pns.idPais    = @idPais
                  AND pns.idNegocio = @idNegocio
                  AND pns.idSistema = @sistema
                GROUP BY feccarga
                ORDER BY feccarga DESC;
            ", new { sistema, idPais, idNegocio });
        }

        public async Task<IEnumerable<Abm_Sistema>> ListaDeSistemas(int idPais, int idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Directo a ftc_pns_2. Mucho más limpio.
                var sql = @"
                SELECT DISTINCT
                   ISNULL(idSistema, 0) idSistema,
                   ISNULL(sistema, 'SIN SISTEMA') sistema
                FROM dbo.ftc_pns_2
                WHERE idPais    = @idPais
                  AND idNegocio = @idNegocio
                ORDER BY ISNULL(sistema, 'SIN SISTEMA');";

                return await dbdapper.QueryAsync<Abm_Sistema>(sql, new { idPais, idNegocio });
            }
        }
    }
}