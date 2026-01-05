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
            return await db.QueryAsync<PaisViewModel>(@"
        SELECT DISTINCT
            ISNULL(b.idPais, 0)        AS IdPais,  
            ISNULL(b.pais, 'SIN PAIS') AS NombrePais  
        FROM (
            SELECT idPaisNegocioSistema, pais, idPais, 'Antigua' vertical, negocio, idNegocio, sistema, codSistema, idSistema, codPais
            FROM pns
            UNION ALL 
            SELECT * FROM ftc_pns_2
        ) b
        WHERE b.idNegocio = @idNegocio
        ORDER BY ISNULL(b.pais, 'SIN PAIS');
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
         isnull(b.sistema,'SIN SISTEMA')                   AS Sistema,
         SUM(gd.cnt_activos)         AS Activos,
         SUM(gd.cnt_finiquitados)    AS Finiquitados,
         SUM(gd.cnt_no_encontrados)  AS No_Encontrados
     FROM dbo.ftc_gestion_diaria AS gd left outer join 
	 ( (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
      from pns
      union all 
      select * from ftc_pns_2)) b
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
     WHERE
         gd.feccarga    = (SELECT feccarga FROM UltimaCarga)
         AND b.idPais      = @idPais
        AND b.idNegocio = @idNegocio
     GROUP BY isnull(b.sistema,'SIN SISTEMA')  
 ),
 GestionDiariaData AS (
     SELECT DISTINCT
         isnull(b.sistema,'SIN SISTEMA') AS Sistema
     FROM dbo.ftc_gestion_diaria AS gd left outer join 
	 (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
      from pns
      union all 
      select * from ftc_pns_2) b
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
     WHERE
         gd.feccarga    = (SELECT feccarga FROM UltimaCarga)
         AND B.idPais      = @idPais
         AND B.idNegocio = @idNegocio
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
                    B.pais,
                    B.sistema,
                    B.idSistema,
                    SUM(gd.Riesgo_Alto) AS Riesgo_Alto, 
                    SUM(gd.Riesgo_Medio) AS Riesgo_Medio, 
                    SUM(gd.Riesgo_Bajo) AS Riesgo_Bajo
                FROM dbo.ftc_gestion_diaria gd left outer join 
	            (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
                WHERE B.idPais       = @idPais
                AND B.idNegocio  = @idNegocio
                GROUP BY B.pais, B.sistema, B.idSistema
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
      UPPER(ISNULL(B.pais,'SIN PAIS'))                   AS pais,
      SUM(gd.cnt_finiquitados)       AS cnt_finiquitados,
      SUM(gd.cnt_no_encontrados)     AS cnt_no_encontrados,
      SUM(gd.cnt_activos)            AS cnt_activos
  FROM dbo.ftc_gestion_diaria AS gd left outer join 
	            (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
        
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
  WHERE 
     gd.feccarga      = (SELECT feccarga FROM UltimaCarga)
      AND B.idPais      = @idPais
      AND B.idNegocio = @idNegocio
  GROUP BY UPPER(ISNULL(B.pais,'SIN PAIS')) 
  ORDER BY UPPER(ISNULL(B.pais,'SIN PAIS')) ;
                ";
				return await dbdapper.QueryAsync<RiesgoPaisViewModel>(sql, new { idPais, idNegocio });
			}
		}

        public async Task<IEnumerable<RiesgoPaisViewModel>> ListaRiesgoPorNegocio(int idNegocio)
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
                        ISNULL(B.idPais,0) AS IdPais,             
                        UPPER(ISNULL(B.pais,'SIN PAIS'))              AS Pais,
                        SUM(gd.cnt_finiquitados)   AS cnt_finiquitados,
                        SUM(gd.cnt_no_encontrados) AS cnt_no_encontrados,
                        SUM(gd.cnt_activos)        AS cnt_activos
                    FROM dbo.ftc_gestion_diaria AS gd  left outer join 
	            (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
        
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
		   WHERE 
                        gd.feccarga   = (SELECT feccarga FROM UltimaCarga)
                        AND B.idNegocio = @idNegocio
                    GROUP BY 
                        ISNULL(B.idPais,0), 
                        UPPER(ISNULL(B.pais,'SIN PAIS'))    
                    ORDER BY 
                        UPPER(ISNULL(B.pais,'SIN PAIS'));
                ";
                return await dbdapper.QueryAsync<RiesgoPaisViewModel>(sql, new { idNegocio });
            }
        }

        public async Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion(int idPais, int idNegocio)
		{
			using var db = new SqlConnection(connectionString);
			return await db.QueryAsync<EvolucionViewModel>(@"
SELECT TOP 30
                feccarga,
                SUM(cnt_finiquitados) AS cantidad_finiquitados
            FROM dbo.ftc_gestion_diaria gd  left outer join 
	            (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
        
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
            WHERE B.idPais    = @idPais
              AND B.idNegocio = @idNegocio
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
            FROM dbo.ftc_gestion_diaria gd  left outer join 
	            (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
        
           on gd.idPaisNegocioSistema = b.idPaisNegocioSistema
            WHERE B.idPais    = @idPais
              AND B.idNegocio = @idNegocio
              AND B.idSistema   = @sistema
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
                   ISNULL( B.idSistema,0) idSistema,
                   ISNULL( B.sistema,'SIN SISTEMA') sistema
               FROM (select idPaisNegocioSistema,pais,idPais,'Antigua' vertical,negocio,idNegocio,sistema,codSistema,idSistema,codPais
					  from pns
					  union all 
					  select * from ftc_pns_2) b
                WHERE B.idPais      = @idPais
                  AND B.idNegocio   = @idNegocio
                ORDER BY ISNULL( B.sistema,'SIN SISTEMA');";
				return await dbdapper.QueryAsync<Abm_Sistema>(sql, new { idPais, idNegocio });
			}
		}

	}
}
