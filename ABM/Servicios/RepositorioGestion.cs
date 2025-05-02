using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
    public interface IRepositorioGestion
    {
        Task<ResumenGestionViewModel> ObtenerDatosGestion(int idpais, int idnegocio);
        Task<IEnumerable<CasosCargoViewModel>> ObtenerListaCasosCargo(int idpais, int idnegocio);
        Task<IEnumerable<EvidenciasFiniquitadoViewModel>> ObtenerListaEvidenciasFiniquitado(int idpais, int idnegocio);
        Task<IEnumerable<ResumenPaisViewModel>> ObtenerListaResumenPais(int idpais, int idnegocio);
        Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaTendenciaDiaria(int idpais, int idnegocio);
    }
    public class RepositorioGestion : IRepositorioGestion
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioGestion(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<ResumenGestionViewModel> ObtenerDatosGestion(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryFirstOrDefaultAsync<ResumenGestionViewModel>(@"
            SELECT DISTINCT
                SUM(subquery.cnt_activos) AS Activos,
                SUM(subquery.cnt_finiquitados) AS Finiquitados,
                SUM(subquery.cnt_no_encontrados) AS NoEncontrados,
                ROUND((CAST(SUM(subquery.cnt_finiquitados) AS FLOAT) * 100) / NULLIF(SUM(subquery.cnt_usuarios), 0), 2) AS Riesgo
            FROM (
                SELECT
                    COUNT(DISTINCT CASE WHEN dbo.ftc_agrupa_activos.estado = 'ACTIVO' THEN dbo.ftc_agrupa_activos.rutdni END) AS cnt_activos,
                    COUNT(DISTINCT CASE WHEN dbo.ftc_agrupa_activos.estado = 'FINIQUITADO' THEN dbo.ftc_agrupa_activos.rutdni END) AS cnt_finiquitados,
                    COUNT(DISTINCT CASE WHEN dbo.ftc_agrupa_activos.estado = 'NO ENCONTRADO' THEN dbo.ftc_agrupa_activos.rutdni END) AS cnt_no_encontrados,
                    COUNT(DISTINCT dbo.ftc_agrupa_activos.rutdni) AS cnt_usuarios,
                    dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema, 
                    dbo.ftc_pais.pais,
                    dbo.ftc_sistema.sistema
                FROM 
                    dbo.ftc_agrupa_activos
                INNER JOIN
                    dbo.ftc_pais_negocio_sistema ON dbo.ftc_agrupa_activos.idPaisNegocioSistema = dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema 
                INNER JOIN 
                    dbo.ftc_pais ON dbo.ftc_pais_negocio_sistema.idPais = dbo.ftc_pais.idPais 
                INNER JOIN 
                    dbo.ftc_negocio ON dbo.ftc_pais_negocio_sistema.idNegocio = dbo.ftc_negocio.idNegocio 
                INNER JOIN 
                    dbo.ftc_sistema ON dbo.ftc_pais_negocio_sistema.idSistema = dbo.ftc_sistema.idSistema 
                INNER JOIN 
                    dbo.ftc_gestion_diaria ON dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema = dbo.ftc_gestion_diaria.idPaisNegocioSistema
                WHERE
                    ftc_gestion_diaria.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM ftc_gestion_diaria 
                        ORDER BY SUBSTRING(feccarga, 7, 4) + SUBSTRING(feccarga, 4, 2) + SUBSTRING(feccarga, 1, 2) DESC
                    )
                    AND dbo.ftc_pais_negocio_sistema.idPais = @idpais
                    AND dbo.ftc_pais_negocio_sistema.idNegocio = @idnegocio
                GROUP BY 
                    dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema, 
                    dbo.ftc_pais.pais,
                    dbo.ftc_sistema.sistema
            ) AS subquery
            GROUP BY 
                subquery.pais;", parametros);

                return modelo;
            }
        }

        public async Task<IEnumerable<CasosCargoViewModel>> ObtenerListaCasosCargo(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryAsync<CasosCargoViewModel>(@"
            SELECT 
                sistema,
                SUM(rut) AS rut, 
                SUM(total) AS casos,  
                SUM(cerrados) AS cerrados,  
                SUM(total) - SUM(cerrados) AS pendientes 
            FROM (
                SELECT 
                    dbo.ftc_sistema.sistema,
                    COUNT(DISTINCT mtd.rutdni) AS rut,
                    COUNT(mtd.estado_ex) AS total,
                    CASE WHEN mtd.estado_ex = 'GESTIONADO' THEN COUNT(mtd.estado_ex) ELSE 0 END AS gestionados,
                    CASE WHEN mtd.estado_ex LIKE 'CERRADO%' THEN COUNT(mtd.estado_ex) ELSE 0 END AS cerrados,
                    dbo.ftc_sistema.idSistema
                FROM ftc_matriz_diaria AS mtd
                INNER JOIN dbo.ftc_pais_negocio_sistema 
                    ON mtd.idPaisNegocioSistema = dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema
                INNER JOIN dbo.ftc_sistema 
                    ON dbo.ftc_pais_negocio_sistema.idSistema = dbo.ftc_sistema.idSistema
                WHERE dbo.ftc_pais_negocio_sistema.idPais = @idpais
                  AND dbo.ftc_pais_negocio_sistema.idNegocio = @idnegocio
                GROUP BY 
                    dbo.ftc_sistema.sistema, 
                    mtd.estado_ex, 
                    dbo.ftc_sistema.idSistema
            ) AS c
            GROUP BY 
                sistema, 
                idSistema;", parametros);

                return modelo;
            }
        }

        public async Task<IEnumerable<EvidenciasFiniquitadoViewModel>> ObtenerListaEvidenciasFiniquitado(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryAsync<EvidenciasFiniquitadoViewModel>(@"
            SELECT        
                ftc_sistema.sistema,
                ftc_gestion_diaria.cnt_finiquitados AS finiquitados_hoy,
                ftc_gestion_diaria.entre_1_3,
                ftc_gestion_diaria.entre_4_6,
                ftc_gestion_diaria.mayor_a_6
            FROM ftc_gestion_diaria
            INNER JOIN ftc_pais_negocio_sistema 
                ON ftc_gestion_diaria.idPaisNegocioSistema = ftc_pais_negocio_sistema.idPaisNegocioSistema  
            INNER JOIN ftc_pais 
                ON ftc_pais_negocio_sistema.idPais = ftc_pais.idPais 
            INNER JOIN ftc_negocio 
                ON ftc_pais_negocio_sistema.idNegocio = ftc_negocio.idNegocio 
            INNER JOIN ftc_sistema 
                ON ftc_pais_negocio_sistema.idSistema = ftc_sistema.idSistema
            WHERE 
                ftc_gestion_diaria.feccarga = (
                    SELECT TOP 1 feccarga 
                    FROM ftc_gestion_diaria 
                    ORDER BY idGestionDiaria DESC
                )
                AND ftc_pais_negocio_sistema.idPais = @idpais
                AND ftc_pais_negocio_sistema.idNegocio = @idnegocio;
        ", parametros);

                return modelo;
            }
        }

        public async Task<IEnumerable<ResumenPaisViewModel>> ObtenerListaResumenPais(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryAsync<ResumenPaisViewModel>(@"
            ;WITH ActivosData AS (
                SELECT 
                    AL3.sistema,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'ACTIVO' THEN AL1.rutdni END) AS ACTIVOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'FINIQUITADO' THEN AL1.rutdni END) AS FINIQUITADOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'NO ENCONTRADO' THEN AL1.rutdni END) AS NO_ENCONTRADOS
                FROM 
                    dbo.ftc_agrupa_activos AL1
                JOIN dbo.ftc_pais_negocio_sistema AL2 
                    ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                JOIN dbo.ftc_sistema AL3 
                    ON AL3.idSistema = AL2.idSistema
                WHERE 
                    AL2.idPais = @idpais
                    AND AL2.idNegocio = @idnegocio
                GROUP BY AL3.sistema
            ),
            GestionDiariaData AS (
                SELECT DISTINCT 
                    AL3.sistema AS Sistema
                FROM
                    dbo.ftc_pais_negocio_sistema AS pns
                INNER JOIN dbo.ftc_gestion_diaria AS gd 
                    ON pns.idPaisNegocioSistema = gd.idPaisNegocioSistema 
                INNER JOIN dbo.ftc_sistema AS AL3 
                    ON pns.idSistema = AL3.idSistema 
                WHERE 
                    gd.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM ftc_gestion_diaria 
                        ORDER BY SUBSTRING(feccarga, 7, 4) + SUBSTRING(feccarga, 4, 2) + SUBSTRING(feccarga, 1, 2) DESC
                    )
                    AND pns.idPais = @idpais
                    AND pns.idNegocio = @idnegocio
            )

            SELECT 
                G.sistema,
                A.ACTIVOS,
                A.FINIQUITADOS,
                A.NO_ENCONTRADOS
            FROM 
                ActivosData A
            LEFT JOIN 
                GestionDiariaData G ON A.sistema = G.Sistema;
        ", parametros);

                return modelo;
            }
        }

        public async Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaTendenciaDiaria(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryAsync<TendenciaDiariaViewModel>(@"
            SELECT TOP 60 
                dbo.ftc_gestion_diaria.feccarga, 
                SUM(dbo.ftc_gestion_diaria.cnt_finiquitados) AS cnt_finiquitados
            FROM dbo.ftc_gestion_diaria
            INNER JOIN dbo.ftc_pais_negocio_sistema 
                ON dbo.ftc_gestion_diaria.idPaisNegocioSistema = dbo.ftc_pais_negocio_sistema.idPaisNegocioSistema
            WHERE 
                dbo.ftc_gestion_diaria.feccarga IS NOT NULL
                AND ftc_pais_negocio_sistema.idPais = @idpais
                AND ftc_pais_negocio_sistema.idNegocio = @idnegocio
            GROUP BY 
                dbo.ftc_gestion_diaria.feccarga
            ORDER BY 
                CONVERT(date, dbo.ftc_gestion_diaria.feccarga) DESC;
        ", parametros);

                return modelo;
            }
        }



    }
}
