using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
	public interface IRepositorioAlertas
	{
        Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios();
	}
	public class RepositorioAlertas : IRepositorioAlertas
	{
		private readonly string connectionString;
		private readonly HttpContext httpContext;

		public RepositorioAlertas(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
		{
			connectionString = configuration.GetConnectionString("CadenaSQL");
			httpContext = httpContextAccessor.HttpContext;
		}

        //FALTA LA COLUMNA PAÍS Y NEGOCIO
		public async Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios()
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<EstadisticasUsuarios>(@"
                    
                WITH ActivosData AS (
                    SELECT 
                        AL3.sistema AS Sistema,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'ACTIVO' THEN AL1.rutdni END) AS Activos,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'FINIQUITADO' THEN AL1.rutdni END) AS Finiquitados,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'NO ENCONTRADO' THEN AL1.rutdni END) AS No_Encontrados,
                        COUNT(DISTINCT CASE WHEN Al1.cta_duplicada = 'SI' THEN AL1.rutdni END) AS CtaDuplicadas,
                        -- Suma de todos los valores anteriores para obtener el total de usuarios
                        COUNT(DISTINCT CASE WHEN Al1.estado IN ('ACTIVO', 'FINIQUITADO', 'NO ENCONTRADO') OR Al1.cta_duplicada = 'SI' THEN AL1.rutdni END) AS total_Usuarios
                    FROM 
                        dbo.ftc_agrupa_activos AL1
                    JOIN dbo.ftc_pais_negocio_sistema AL2 
                        ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                    JOIN dbo.ftc_sistema AL3 
                        ON AL3.idSistema = AL2.idSistema
                    JOIN dbo.ftc_pais AL4 
                        ON AL4.idPais = AL2.idPais
                    GROUP BY AL3.sistema
                ),
                GestionDiariaData AS (
                    SELECT DISTINCT 
                        ftc_sistema.sistema AS Sistema,
                        ISNULL(ftc_gestion_diaria.cnt_recontratados, 0) AS Recontratados,
                        ISNULL(ftc_gestion_diaria.entre_1_3, 0) AS De_1_a_3_Dias_Sin_Gestion,
                        ISNULL(ftc_gestion_diaria.entre_4_6, 0) AS De_4_a_6_Dias_Sin_Gestion,
                        ISNULL(ftc_gestion_diaria.mayor_a_6, 0) AS Mas_de_6_Dias_Sin_Gestion
                    FROM
                        ftc_pais_negocio_sistema 
                    INNER JOIN ftc_gestion_diaria 
                        ON ftc_pais_negocio_sistema.idPaisNegocioSistema = ftc_gestion_diaria.idPaisNegocioSistema 
                    INNER JOIN ftc_pais 
                        ON ftc_pais_negocio_sistema.idPais = ftc_pais.idPais 
                    INNER JOIN ftc_sistema 
                        ON ftc_pais_negocio_sistema.idSistema = ftc_sistema.idSistema 
                    WHERE 
                        ftc_gestion_diaria.feccarga = (select top 1 feccarga from ftc_gestion_diaria order by SUBSTRING(feccarga,7,4)+SUBSTRING(feccarga,4,2)+ SUBSTRING(feccarga,1,2) desc )
                )

                SELECT 
                    A.Sistema,
                    A.total_Usuarios AS TotalUsuarios,
                    A.Activos,
                    A.Finiquitados,
                    A.No_Encontrados AS NoEncontrados,
                    A.CtaDuplicadas,
                    G.Recontratados,
                    G.De_1_a_3_Dias_Sin_Gestion AS De1a3DiasSinGestion,
                    G.De_4_a_6_Dias_Sin_Gestion AS De4a6DiasSinGestion,
                    G.Mas_de_6_Dias_Sin_Gestion AS MasDe6DiasSinGestion
                FROM 
                    ActivosData A
                LEFT JOIN 
                    GestionDiariaData G 
                    ON A.Sistema = G.Sistema;
                ");
			}
		}


	}
}
