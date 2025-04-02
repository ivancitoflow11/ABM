using Dapper;
using ABM.Data;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;
namespace ABM.Servicios
{
    public interface IRepositorioAlertaSistema
    {
        Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios();
        Task<IEnumerable<Finiquitados>> ObtenerDetalleFiniquitados();
        Task<IEnumerable<UsuariosNoEncontrados>> ObtenerDetalleNoEncontrados();
        Task<IEnumerable<UsuariosDuplicados>> ObtenerDetalleDuplicados();
    }
    public class RepositorioAlertaSistema : IRepositorioAlertaSistema
    {
        private readonly string connectionString;
        public RepositorioAlertaSistema(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }



        public async Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<EstadisticasUsuarios>(@"
                    
                ;WITH ActivosData AS (
                    SELECT 
                        AL3.sistema AS Sistema,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'ACTIVO' THEN AL1.rutdni END) AS Activos,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'FINIQUITADO' THEN AL1.rutdni END) AS Finiquitados,
                        COUNT(DISTINCT CASE WHEN Al1.estado = 'NO ENCONTRADO' THEN AL1.rutdni END) AS No_Encontrados,
                        COUNT(DISTINCT CASE WHEN Al1.cta_duplicada = 'SI' THEN AL1.rutdni END) AS CtaDuplicadas,
                        -- Suma de todos los valores anteriores para obtener el total de usuarios
                        COUNT(DISTINCT CASE WHEN Al1.estado IN ('ACTIVO', 'FINIQUITADO', 'NO ENCONTRADO') OR Al1.cta_duplicada = 'SI' THEN AL1.rutdni END) AS total_Usuarios
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
                        sistema.sistema AS Sistema,
                        ISNULL(gestion_diaria.cnt_recontratados, 0) AS Recontratados,
                        ISNULL(gestion_diaria.entre_1_3, 0) AS De_1_a_3_Dias_Sin_Gestion,
                        ISNULL(gestion_diaria.entre_4_6, 0) AS De_4_a_6_Dias_Sin_Gestion,
                        ISNULL(gestion_diaria.mayor_a_6, 0) AS Mas_de_6_Dias_Sin_Gestion
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


        public async Task<IEnumerable<Finiquitados>> ObtenerDetalleFiniquitados()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Finiquitados>(@"
                    
                SELECT AL4.pais, AL3.sistema, AL3.codSistema,AL1.idPaisNegocioSistema, 
                                                                    AL1.rutdni, AL1.dv, AL1.nombreusuario, AL1.userid, 
                                                                    AL1.mailusuario, AL1.cargospr, AL1.perfil, AL1.cargo,
                                                                    AL1.codcosto, AL1.codccostospr, AL1.Nomccostospr, 
                                                                    AL1.codccosto, AL1.Nomccosto, AL1.fecalta, AL1.fecbaja,
                                                                    AL1.fecact, AL1.fecultlogin, AL1.ctasfallidas, AL1.estado, 
                                                                    AL1.fecfiniq, AL1.feccargafiniq, AL1.cargomatriz,
                                                                    AL1.perfilmatriz, AL1.feccarga, AL1.empresa,
                                                                    AL1.cta_duplicada, AL1.fechaad 
                                                                    FROM dbo.im_agrupa_activos AL1, dbo.pais_negocio_sistema AL2, 
                                                                    dbo.sistema AL3, dbo.pais AL4 WHERE 
                                                                    (AL2.idPaisNegocioSistema=AL1.idPaisNegocioSistema AND AL3.idSistema=AL2.idSistema AND AL4.idPais=AL2.idPais)
                                                                    AND (AL4.pais='IMPERIAL')
                                                                    and AL1.estado = 'FINIQUITADO'
                ");
            }
        }


        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerDetalleNoEncontrados()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(@"
                    
                SELECT AL4.pais, AL3.sistema, AL3.codSistema,AL1.idPaisNegocioSistema, 
                                                                    AL1.rutdni, AL1.dv, AL1.nombreusuario, AL1.userid, 
                                                                    AL1.mailusuario, AL1.cargospr, AL1.perfil, AL1.cargo,
                                                                    AL1.codcosto, AL1.codccostospr, AL1.Nomccostospr, 
                                                                    AL1.codccosto, AL1.Nomccosto, AL1.fecalta, AL1.fecbaja,
                                                                    AL1.fecact, AL1.fecultlogin, AL1.ctasfallidas, AL1.estado, 
                                                                    AL1.fecfiniq, AL1.feccargafiniq, AL1.cargomatriz,
                                                                    AL1.perfilmatriz, AL1.feccarga, AL1.empresa,
                                                                    AL1.cta_duplicada, AL1.fechaad 
                                                                    FROM dbo.im_agrupa_activos AL1, dbo.pais_negocio_sistema AL2, 
                                                                    dbo.sistema AL3, dbo.pais AL4 WHERE 
                                                                    (AL2.idPaisNegocioSistema=AL1.idPaisNegocioSistema AND AL3.idSistema=AL2.idSistema AND AL4.idPais=AL2.idPais)
                                                                    AND (AL4.pais='IMPERIAL')
                                                                    and AL1.estado = 'NO ENCONTRADO'
                ");
            }
        }


        public async Task<IEnumerable<UsuariosDuplicados>> ObtenerDetalleDuplicados()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosDuplicados>(@"
                    
                WITH CTE_Cuentas AS (
                    SELECT 
                        AL4.pais, AL3.sistema, AL3.codSistema, AL1.idPaisNegocioSistema, 
                        AL1.rutdni, AL1.dv, AL1.nombreusuario, AL1.userid, 
                        AL1.mailusuario, AL1.cargospr, AL1.perfil, AL1.cargo,
                        AL1.codcosto, AL1.codccostospr, AL1.Nomccostospr, 
                        AL1.codccosto, AL1.Nomccosto, AL1.fecalta, AL1.fecbaja,
                        AL1.fecact, AL1.fecultlogin, AL1.ctasfallidas, AL1.estado, 
                        AL1.fecfiniq, AL1.feccargafiniq, AL1.cargomatriz,
                        AL1.perfilmatriz, AL1.feccarga, AL1.empresa,
                        AL1.cta_duplicada, AL1.fechaad,
                        ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.fechaad DESC) AS RowNum
                    FROM dbo.im_agrupa_activos AL1
                    JOIN dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                    JOIN dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
                    JOIN dbo.pais AL4 ON AL4.idPais = AL2.idPais
                    WHERE AL4.pais = 'IMPERIAL'
                      AND AL1.cta_duplicada = 'SI'
                      AND AL2.idSistema IN ('2', '1')
                )
                SELECT *
                FROM CTE_Cuentas
                WHERE RowNum = 1;
                ");
            }
        }


    }
}
