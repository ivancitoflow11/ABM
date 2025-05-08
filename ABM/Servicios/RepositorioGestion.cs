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
            SELECT
                sub.feccarga                         AS FechaCarga,
                sub.pais,
                SUM(sub.cnt_activos)       AS Activos,
                SUM(sub.cnt_finiquitados)  AS Finiquitados,
                SUM(sub.cnt_no_encontrados)AS NoEncontrados,
                ROUND(
                  CAST(SUM(sub.cnt_finiquitados) AS FLOAT) * 100
                  / NULLIF(SUM(sub.cnt_usuarios), 0),
                2)                          AS Riesgo
            FROM (
                SELECT
                    gd.feccarga,
                    gd.cnt_activos,
                    gd.cnt_finiquitados,
                    gd.cnt_no_encontrados,
                    gd.cnt_usuarios,
                    p.pais
                FROM dbo.ftc_gestion_diaria AS gd
                INNER JOIN dbo.ftc_pais_negocio_sistema AS pns
                    ON gd.idPaisNegocioSistema = pns.idPaisNegocioSistema  
                INNER JOIN dbo.ftc_pais AS p
                    ON pns.idPais = p.idPais  
                WHERE
                    gd.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM dbo.ftc_gestion_diaria 
                        ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
                    )
                    AND pns.idPais    = @idpais
                    AND pns.idNegocio = @idnegocio
            ) AS sub
            GROUP BY
                sub.feccarga,
                sub.pais
            ORDER BY
                sub.pais;", parametros);

                return modelo;
            }
        }

        //NO VA POR AHORA
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
                    AL1.feccarga                          AS feccarga,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'ACTIVO'         THEN AL1.rutdni END) AS ACTIVOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'FINIQUITADO'    THEN AL1.rutdni END) AS FINIQUITADOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'NO ENCONTRADO'  THEN AL1.rutdni END) AS NO_ENCONTRADOS
                FROM dbo.ftc_agrupa_activos AS AL1
                INNER JOIN dbo.ftc_pais_negocio_sistema AS AL2 
                    ON AL1.idPaisNegocioSistema = AL2.idPaisNegocioSistema
                INNER JOIN dbo.ftc_sistema AS AL3 
                    ON AL2.idSistema = AL3.idSistema
                WHERE 
                    AL2.idPais     = @idpais
                    AND AL2.idNegocio = @idnegocio
                GROUP BY 
                    AL3.sistema,
                    AL1.feccarga
            ),
            GestionDiariaData AS (
                SELECT DISTINCT 
                    AL3.sistema   AS Sistema,
                    AL1.feccarga  AS feccarga
                FROM dbo.ftc_agrupa_activos AS AL1
                INNER JOIN dbo.ftc_pais_negocio_sistema AS pns 
                    ON AL1.idPaisNegocioSistema = pns.idPaisNegocioSistema  
                INNER JOIN dbo.ftc_sistema AS AL3 
                    ON pns.idSistema = AL3.idSistema
                WHERE 
                    AL1.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM dbo.ftc_agrupa_activos
                        ORDER BY TRY_CONVERT(date, feccarga, 23) DESC
                    )
                    AND pns.idPais     = @idpais
                    AND pns.idNegocio  = @idnegocio
            )
            SELECT 
                G.sistema,
                G.feccarga,
                A.ACTIVOS,
                A.FINIQUITADOS,
                A.NO_ENCONTRADOS
            FROM ActivosData AS A
            LEFT JOIN GestionDiariaData AS G 
                ON A.sistema = G.Sistema
            ORDER BY G.sistema;
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
