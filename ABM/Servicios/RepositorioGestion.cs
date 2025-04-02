using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioGestion
    {
        Task<ResumenGestionViewModel> ObtenerDatosGestion();
        Task<IEnumerable<CasosCargoViewModel>> ObtenerListaCasosCargo();
        Task<IEnumerable<EvidenciasFiniquitadoViewModel>> ObtenerListaEvidenciasFiniquitado();
        Task<IEnumerable<ResumenPaisViewModel>> ObtenerListaResumenPais();
        Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaTendenciaDiaria();
    }
    public class RepositorioGestion :IRepositorioGestion
    {
        private readonly string connectionString;

        public RepositorioGestion(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }
        
        public async Task<ResumenGestionViewModel> ObtenerDatosGestion()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryFirstOrDefaultAsync<ResumenGestionViewModel>(@"SELECT DISTINCT
                    SUM(subquery.cnt_activos) AS Activos,
                    SUM(subquery.cnt_finiquitados) AS Finiquitados,
                    SUM(subquery.cnt_no_encontrados) AS NoEncontrados,
                    ROUND((CAST(SUM(subquery.cnt_finiquitados) AS FLOAT) * 100) / NULLIF(SUM(subquery.cnt_usuarios), 0), 2) AS Riesgo
                FROM (
                    SELECT
                        COUNT(DISTINCT CASE WHEN dbo.im_agrupa_activos.estado = 'ACTIVO' THEN dbo.im_agrupa_activos.rutdni END) AS cnt_activos,
                        COUNT(DISTINCT CASE WHEN dbo.im_agrupa_activos.estado = 'FINIQUITADO' THEN dbo.im_agrupa_activos.rutdni END) AS cnt_finiquitados,
                        COUNT(DISTINCT CASE WHEN dbo.im_agrupa_activos.estado = 'NO ENCONTRADO' THEN dbo.im_agrupa_activos.rutdni END) AS cnt_no_encontrados,
                        COUNT(DISTINCT dbo.im_agrupa_activos.rutdni) AS cnt_usuarios,
                        dbo.pais_negocio_sistema.idPaisNegocioSistema, 
                        dbo.pais.pais,
                        dbo.sistema.sistema
                    FROM 
                        dbo.im_agrupa_activos
                    INNER JOIN
                        dbo.pais_negocio_sistema ON dbo.im_agrupa_activos.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema 
                    INNER JOIN 
                        dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
                    INNER JOIN 
                        dbo.negocio ON dbo.pais_negocio_sistema.idNegocio = dbo.negocio.idNegocio 
                    INNER JOIN 
                        dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                    INNER JOIN 
                        dbo.gestion_diaria ON dbo.pais_negocio_sistema.idPaisNegocioSistema = dbo.gestion_diaria.idPaisNegocioSistema
                    WHERE
                        gestion_diaria.feccarga = (SELECT TOP 1 feccarga FROM gestion_diaria ORDER BY SUBSTRING(feccarga, 7, 4) + SUBSTRING(feccarga, 4, 2) + SUBSTRING(feccarga, 1, 2) DESC)
                    GROUP BY 
                        dbo.pais_negocio_sistema.idPaisNegocioSistema, 
                        dbo.pais.pais,
                        dbo.sistema.sistema
                ) AS subquery
                GROUP BY 
                    subquery.pais;
                        ");
                return modelo;
            }
        }
        public async Task<IEnumerable<CasosCargoViewModel>> ObtenerListaCasosCargo()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<CasosCargoViewModel>(@"select sistema,sum(rut) as rut, sum(total) as casos,  
                                                            sum(cerrados) as cerrados,  sum(total) - sum(cerrados) as pendientes from(
                                                        SELECT dbo.sistema.sistema
                                                               ,count(distinct(mtd.rutdni)) as rut
                                                        , COUNT(mtd.estado_ex) AS TOTAL
                                                        , CASE WHEN mtd.estado_ex = 'GESTIONADO' THEN COUNT(mtd.estado_ex) ELSE 0 END AS gestionados
                                                        , CASE WHEN mtd.estado_ex like 'CERRADO%' THEN COUNT(mtd.estado_ex) ELSE 0 END AS CERRADOS
                                                        , dbo.sistema.idSistema
                                                        FROM          im_matriz_diaria AS mtd INNER JOIN
                                                                              dbo.pais_negocio_sistema ON mtd.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema INNER JOIN
                                                                              dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                                                        where --dbo.detalle_rol .idrol  = @rol
                                                        pais_negocio_sistema.idNegocio =1
                                                        GROUP BY dbo.sistema.sistema, mtd.estado_ex, dbo.sistema.idSistema) as c
                                                        group by sistema,idSistema
                                                    ");
                return modelo;
            }
        }
        public async Task<IEnumerable<EvidenciasFiniquitadoViewModel>> ObtenerListaEvidenciasFiniquitado()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<EvidenciasFiniquitadoViewModel>(@"SELECT        
                                                    sistema.sistema
                                                    , gestion_diaria.cnt_finiquitados as finiquitados_hoy
                                                    , gestion_diaria.entre_1_3
                                                    , gestion_diaria.entre_4_6
                                                    , gestion_diaria.mayor_a_6

                                                FROM            gestion_diaria INNER JOIN
                                                                         pais_negocio_sistema ON gestion_diaria.idPaisNegocioSistema = pais_negocio_sistema.idPaisNegocioSistema  INNER JOIN
                                                                         pais ON pais_negocio_sistema.idPais = pais.idPais INNER JOIN
                                                                         negocio ON pais_negocio_sistema.idNegocio = negocio.idNegocio INNER JOIN
                                                                         sistema ON pais_negocio_sistema.idSistema = sistema.idSistema
                                                WHERE        (gestion_diaria.feccarga = (SELECT top 1 (feccarga) FROM gestion_diaria order by idGestionDiaria desc)) 
                                                AND (pais_negocio_sistema.idpais = 1)
                                                ");
                return modelo;
            }
        }
        public async Task<IEnumerable<ResumenPaisViewModel>> ObtenerListaResumenPais()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<ResumenPaisViewModel>(@" ;WITH ActivosData AS (
                SELECT 
                    AL3.sistema,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'ACTIVO' THEN AL1.rutdni END) AS ACTIVOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'FINIQUITADO' THEN AL1.rutdni END) AS FINIQUITADOS,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'NO ENCONTRADO' THEN AL1.rutdni END) AS NO_ENCONTRADOS
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
                    AL3.sistema AS Sistema
                FROM
                    dbo.pais_negocio_sistema AS pns
                INNER JOIN dbo.gestion_diaria AS gd 
                    ON pns.idPaisNegocioSistema = gd.idPaisNegocioSistema 
                INNER JOIN dbo.pais AS p 
                    ON pns.idPais = p.idPais 
                INNER JOIN dbo.sistema AS AL3 ON pns.idSistema = AL3.idSistema 
                WHERE 
                    gd.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM gestion_diaria 
                        ORDER BY SUBSTRING(feccarga, 7, 4) + SUBSTRING(feccarga, 4, 2) + SUBSTRING(feccarga, 1, 2) DESC
                    )
                    AND p.pais = 'IMPERIAL'
            )

            SELECT 
                G.sistema,
                A.ACTIVOS,
                A.FINIQUITADOS,
                A.NO_ENCONTRADOS AS NO_ENCONTRADOS
            FROM 
                ActivosData A
            LEFT JOIN 
                GestionDiariaData G 
            ON 
                A.sistema = G.Sistema;
                                                    ");
                return modelo;
            }
        }
        public async Task<IEnumerable<TendenciaDiariaViewModel>> ObtenerListaTendenciaDiaria()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<TendenciaDiariaViewModel>(@"SELECT  top 60 dbo.gestion_diaria.feccarga, sum(dbo.gestion_diaria.cnt_finiquitados) as cnt_finiquitados
                                                                                FROM            dbo.gestion_diaria INNER JOIN
                                                                                                        dbo.pais_negocio_sistema ON dbo.gestion_diaria.idPaisNegocioSistema = dbo.pais_negocio_sistema.idPaisNegocioSistema
                                                                                WHERE
                                                                                       (dbo.gestion_diaria.feccarga IS NOT NULL) 
                                                                                        and pais_negocio_sistema.idPais= 1
      
                                                                                GROUP BY dbo.gestion_diaria.feccarga
                                                                                ORDER BY convert(date,  dbo.gestion_diaria.feccarga) desc
                                                                                ");
                return modelo;
            }
        }
    }
}
