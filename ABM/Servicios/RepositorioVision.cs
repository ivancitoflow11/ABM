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
			return await db.QueryAsync<PaisViewModel>(@"
            SELECT DISTINCT
                p.idPais   AS IdPais,
                p.pais     AS NombrePais
            FROM dbo.ftc_pais_negocio_sistema pns
            JOIN dbo.ftc_pais p ON p.idPais = pns.idPais
            WHERE pns.idNegocio = @idNegocio
            ORDER BY p.pais;
        ", new { idNegocio });
		}

		public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerListaRiesgoSistema(int idPais, int idNegocio)
		{
			using var db = new SqlConnection(connectionString);
			return await db.QueryAsync<RiesgoSistemaViewModel>(@"
            WITH UltimaCarga AS (
                SELECT TOP 1 feccarga
                FROM dbo.ftc_gestion_diaria
                ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
            ),
            ActivosData AS (
                SELECT
                    s.sistema                   AS Sistema,
                    SUM(gd.cnt_activos)         AS Activos,
                    SUM(gd.cnt_finiquitados)    AS Finiquitados,
                    SUM(gd.cnt_no_encontrados)  AS No_Encontrados
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pais_negocio_sistema AS pns
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                INNER JOIN dbo.ftc_pais AS p
                    ON pns.idPais = p.idPais
                INNER JOIN dbo.ftc_sistema AS s
                    ON pns.idSistema = s.idSistema
                WHERE
                    gd.feccarga    = (SELECT feccarga FROM UltimaCarga)
                    AND p.idPais      = @idPais
                    AND pns.idNegocio = @idNegocio
                GROUP BY s.sistema
            ),
            GestionDiariaData AS (
                SELECT DISTINCT
                    s.sistema AS Sistema
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pais_negocio_sistema AS pns
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                INNER JOIN dbo.ftc_pais AS p
                    ON pns.idPais = p.idPais
                INNER JOIN dbo.ftc_sistema AS s
                    ON pns.idSistema = s.idSistema
                WHERE
                    gd.feccarga    = (SELECT feccarga FROM UltimaCarga)
                    AND p.idPais      = @idPais
                    AND pns.idNegocio = @idNegocio
            )
            SELECT
                A.Sistema,
                A.Activos,
                A.Finiquitados,
                A.No_Encontrados AS NoEncontrados
            FROM ActivosData AS A
            LEFT JOIN GestionDiariaData AS G
                ON A.Sistema = G.Sistema
            ORDER BY A.Sistema;

        ", new { idPais, idNegocio });
		}

		public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerRiesgoPerfil(int idPais, int idNegocio)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				var sql = @"
                SELECT TOP 2   
                    p.pais,
                    s.sistema,
                    s.nriesgo,
                    SUM(gd.Riesgo_Alto) AS Riesgo_Alto, 
                    SUM(gd.Riesgo_Medio) AS Riesgo_Medio, 
                    SUM(gd.Riesgo_Bajo) AS Riesgo_Bajo
                FROM dbo.ftc_gestion_diaria gd
                INNER JOIN dbo.ftc_pais_negocio_sistema pns 
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema 
                INNER JOIN dbo.ftc_pais p 
                    ON pns.idPais   = p.idPais 
                INNER JOIN dbo.ftc_negocio n 
                    ON pns.idNegocio= n.idNegocio 
                INNER JOIN dbo.ftc_sistema s 
                    ON pns.idSistema= s.idSistema 
                WHERE p.idPais       = @idPais
                  AND pns.idNegocio  = @idNegocio
                GROUP BY p.pais, s.sistema, s.nriesgo
                ORDER BY MAX(gd.feccarga) DESC;
                ";
				return await dbdapper.QueryAsync<RiesgoSistemaViewModel>(sql, new { idPais, idNegocio });
			}
		}

		public async Task<IEnumerable<RiesgoPaisViewModel>> ObtenerListaRiesgoPais(int idPais, int idNegocio)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				var sql = @"
                WITH UltimaCarga AS (
                    SELECT TOP 1 feccarga
                    FROM dbo.ftc_gestion_diaria
                    ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
                )
                SELECT
                    UPPER(p.pais)                   AS pais,
                    SUM(gd.cnt_finiquitados)       AS cnt_finiquitados,
                    SUM(gd.cnt_no_encontrados)     AS cnt_no_encontrados,
                    SUM(gd.cnt_activos)            AS cnt_activos
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pais_negocio_sistema AS pns
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema
                INNER JOIN dbo.ftc_pais AS p
                    ON pns.idPais = p.idPais
                INNER JOIN dbo.ftc_negocio AS n
                    ON pns.idNegocio = n.idNegocio
                INNER JOIN dbo.ftc_sistema AS s
                    ON pns.idSistema = s.idSistema
                WHERE 
                    gd.feccarga      = (SELECT feccarga FROM UltimaCarga)
                    AND p.idPais      = @idPais
                    AND pns.idNegocio = @idNegocio
                GROUP BY p.pais
                ORDER BY p.pais;
                ";
				return await dbdapper.QueryAsync<RiesgoPaisViewModel>(sql, new { idPais, idNegocio });
			}
		}
		public async Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion(int idPais, int idNegocio)
		{
			using var db = new SqlConnection(connectionString);
			return await db.QueryAsync<EvolucionViewModel>(@"
            SELECT TOP 30
                feccarga,
                SUM(cnt_finiquitados) AS cantidad_finiquitados
            FROM dbo.ftc_gestion_diaria gd
            JOIN dbo.ftc_pais_negocio_sistema pns 
                ON pns.idPaisNegocioSistema = gd.idPaisNegocioSistema
            WHERE pns.idPais    = @idPais
              AND pns.idNegocio = @idNegocio
            GROUP BY feccarga
            ORDER BY feccarga DESC;
        ", new { idPais, idNegocio });
		}

		public async Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaEvolucionParametro(int sistema, int idPais, int idNegocio)
		{
			using var db = new SqlConnection(connectionString);
			return await db.QueryAsync<TendenciaDiariaViewModel>(@"
            SELECT TOP 30
                feccarga,
                SUM(cnt_finiquitados) AS cantidad_finiquitados
            FROM dbo.ftc_gestion_diaria gd
            JOIN dbo.ftc_pais_negocio_sistema pns 
                ON pns.idPaisNegocioSistema = gd.idPaisNegocioSistema
            JOIN dbo.ftc_sistema s 
                ON s.idSistema = pns.idSistema
            WHERE pns.idPais    = @idPais
              AND pns.idNegocio = @idNegocio
              AND s.idSistema   = @sistema
            GROUP BY feccarga
            ORDER BY feccarga DESC;
        ", new { sistema, idPais, idNegocio });
		}

		public async Task<IEnumerable<Abm_Sistema>> ListaDeSistemas(int idPais, int idNegocio)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				var sql = @"
                SELECT DISTINCT
                    s.idSistema,
                    s.sistema
                FROM dbo.ftc_pais_negocio_sistema pns
                JOIN dbo.ftc_sistema s 
                    ON pns.idSistema = s.idSistema
                WHERE pns.idPais      = @idPais
                  AND pns.idNegocio   = @idNegocio
                ORDER BY s.sistema;";
				return await dbdapper.QueryAsync<Abm_Sistema>(sql, new { idPais, idNegocio });
			}
		}

	}
}
