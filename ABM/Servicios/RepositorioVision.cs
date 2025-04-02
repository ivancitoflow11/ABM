using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioVision
    {
        Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion();
        Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaEvolucionParametro(int sistema);
        Task<IEnumerable<RiesgoPaisViewModel>> ObtenerListaRiesgoPais();
        Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerListaRiesgoSistema();
        Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerRiesgoPerfil();
    }
    public class RepositorioVision : IRepositorioVision
    {
        private readonly string connectionString;

        public RepositorioVision(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }
        public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerListaRiesgoSistema()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<RiesgoSistemaViewModel>(@";WITH ActivosData AS (
                    SELECT 
                        AL3.sistema AS Sistema,
                        COUNT(DISTINCT CASE WHEN AL1.estado = 'ACTIVO' THEN AL1.rutdni END) AS Activos,
                        COUNT(DISTINCT CASE WHEN AL1.estado = 'FINIQUITADO' THEN AL1.rutdni END) AS Finiquitados,
                        COUNT(DISTINCT CASE WHEN AL1.estado = 'NO ENCONTRADO' THEN AL1.rutdni END) AS No_Encontrados
                    FROM 
                        dbo.im_agrupa_activos AL1
                    JOIN dbo.pais_negocio_sistema AL2 
                        ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                    JOIN dbo.sistema AL3 
                        ON AL3.idSistema = AL2.idSistema
                    JOIN dbo.pais AL4 
                        ON AL4.idPais = AL2.idPais
                    WHERE 
                        AL4.pais = 'IMPERIAL'
                    GROUP BY AL3.sistema
                    ),
                    GestionDiariaData AS (
                        SELECT DISTINCT 
                            sistema.sistema AS Sistema
                        FROM
                            pais_negocio_sistema 
                        INNER JOIN gestion_diaria 
                            ON pais_negocio_sistema.idPaisNegocioSistema = gestion_diaria.idPaisNegocioSistema 
                        INNER JOIN pais 
                            ON pais_negocio_sistema.idPais = pais.idPais 
                        INNER JOIN sistema 
                            ON pais_negocio_sistema.idSistema = sistema.idSistema 
                        WHERE 
                            gestion_diaria.feccarga = (select top 1 feccarga from gestion_diaria order by SUBSTRING(feccarga,7,4)+SUBSTRING(feccarga,4,2)+ SUBSTRING(feccarga,1,2) desc )
                            AND pais.pais = 'IMPERIAL'
                    )

                    SELECT 
                        A.Sistema,
                        A.Activos,
                        A.Finiquitados,
                        A.No_Encontrados AS NoEncontrados
                    FROM 
                        ActivosData A
                    LEFT JOIN 
                        GestionDiariaData G 
                        ON A.Sistema = G.Sistema;
                ");
                return modelo;
            }
        }

        public async Task<IEnumerable<RiesgoSistemaViewModel>> ObtenerRiesgoPerfil()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<RiesgoSistemaViewModel>(@"
        SELECT TOP 2   
            dbo.pais.pais,
            dbo.sistema.sistema,
            dbo.sistema.nriesgo,
            SUM(dbo.gestion_diaria.Riesgo_Alto) AS Riesgo_Alto, 
            SUM(dbo.gestion_diaria.Riesgo_Medio) AS Riesgo_Medio, 
            SUM(dbo.gestion_diaria.Riesgo_Bajo) AS Riesgo_Bajo
        FROM            
            dbo.gestion_diaria 
        INNER JOIN dbo.pais_negocio_sistema ON dbo.gestion_diaria.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema 
        INNER JOIN dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
        INNER JOIN dbo.negocio ON dbo.pais_negocio_sistema.idNegocio = dbo.negocio.idNegocio 
        INNER JOIN dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
        WHERE pais.idPais = 1
        GROUP BY dbo.pais.pais, dbo.sistema.sistema, dbo.sistema.nriesgo
        ORDER BY MAX(dbo.gestion_diaria.feccarga) DESC
        ");
                return modelo;
            }
        }

        public async Task<IEnumerable<RiesgoPaisViewModel>> ObtenerListaRiesgoPais()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<RiesgoPaisViewModel>(@"SELECT DISTINCT
                    UPPER(subquery.pais) AS pais, 
                    COUNT(DISTINCT subquery.cnt_finiquitados) AS cnt_finiquitados,
                    COUNT(DISTINCT subquery.cnt_no_encontrados) AS cnt_no_encontrados,
                    SUM(subquery.cnt_activos) AS cnt_activos
                FROM (
                    SELECT 
                        dbo.pais_negocio_sistema.idPaisNegocioSistema, 
                        dbo.pais.pais,
                        COUNT(DISTINCT dbo.gestion_diaria.cnt_finiquitados) AS cnt_finiquitados,
                        COUNT(DISTINCT dbo.gestion_diaria.cnt_no_encontrados) AS cnt_no_encontrados,
                        COUNT(DISTINCT CASE WHEN dbo.im_agrupa_activos.estado = 'ACTIVO' THEN dbo.im_agrupa_activos.rutdni END) AS cnt_activos
                    FROM 
                        dbo.gestion_diaria 
                    INNER JOIN
                        dbo.pais_negocio_sistema ON dbo.gestion_diaria.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema 
                    INNER JOIN 
                        dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
                    INNER JOIN 
                        dbo.negocio ON dbo.pais_negocio_sistema.idNegocio = dbo.negocio.idNegocio 
                    INNER JOIN 
                        dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                    INNER JOIN 
                        dbo.im_agrupa_activos ON dbo.pais_negocio_sistema.idPaisNegocioSistema = dbo.im_agrupa_activos.idPaisNegocioSistema
                    WHERE
                        gestion_diaria.feccarga = (select top 1 feccarga from gestion_diaria order by SUBSTRING(feccarga,7,4)+SUBSTRING(feccarga,4,2)+ SUBSTRING(feccarga,1,2) desc )
                    GROUP BY 
                        dbo.pais_negocio_sistema.idPaisNegocioSistema, 
                        dbo.pais.pais
                ) AS subquery
                GROUP BY 
                    subquery.pais;");
                return modelo;
            }
        }
        public async Task<IEnumerable<EvolucionViewModel>> ObtenerListaEvolucion()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<EvolucionViewModel>(@"SELECT  TOP 30       
                                                                    dbo.gestion_diaria.feccarga, 
                                                                    SUM(dbo.gestion_diaria.cnt_finiquitados) AS cantidad_finiquitados
                                                                    FROM dbo.gestion_diaria 
                                                                    INNER JOIN dbo.pais_negocio_sistema ON dbo.gestion_diaria.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema 
                                                                    INNER JOIN dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
                                                                    INNER JOIN dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                                                                    WHERE 
                                                                    pais.idPais=1
                                                                    GROUP BY dbo.gestion_diaria.feccarga
                                                                    ORDER BY dbo.gestion_diaria.feccarga DESC");
                return modelo;
            }
        }
        public async Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaEvolucionParametro(int sistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<TendenciaDiariaViewModel>(@"SELECT  TOP 30       
                                                                    dbo.gestion_diaria.feccarga, 
                                                                    SUM(dbo.gestion_diaria.cnt_finiquitados) AS cantidad_finiquitados
                                                                    FROM dbo.gestion_diaria 
                                                                    INNER JOIN dbo.pais_negocio_sistema ON dbo.gestion_diaria.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema 
                                                                    INNER JOIN dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
                                                                    INNER JOIN dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                                                                    WHERE 
                                                                    pais.idPais=1 AND sistema.idSistema = @sistema
                                                                    GROUP BY dbo.gestion_diaria.feccarga
                                                                    ORDER BY dbo.gestion_diaria.feccarga DESC", new { sistema });
                return modelo;
            }
        }

    }
}
