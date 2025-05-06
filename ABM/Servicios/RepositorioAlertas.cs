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
        Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios(int? idNegocio, int? idSistema);
        Task<IEnumerable<Finiquitados>> ObtenerDetalleFiniquitados(int? idNegocio, int? idSistema);
        Task<IEnumerable<UsuariosNoEncontrados>> ObtenerDetalleNoEncontrados(int? idNegocio, int? idSistema);
        Task<IEnumerable<UsuariosDuplicados>> ObtenerDetalleDuplicados(int? idNegocio, int? idSistema);
        Task<IEnumerable<Sistema>> ObtenerSistemasPorNegocio(int? idNegocio);
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

        public async Task<IEnumerable<EstadisticasUsuarios>> ObtenerEstadisticasUsuarios(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<EstadisticasUsuarios>(@"
            
        WITH ActivosData AS (
            SELECT 
                AL3.sistema AS Sistema,
                AL5.negocio AS Negocio,
                AL4.pais AS Pais,
                COUNT(CASE WHEN AL1.estado = 'ACTIVO' THEN 1 END) AS Activos,
                COUNT(CASE WHEN AL1.estado = 'FINIQUITADO' THEN 1 END) AS Finiquitados,
                COUNT(CASE WHEN AL1.estado = 'NO ENCONTRADO' THEN 1 END) AS No_Encontrados,
                COUNT(CASE WHEN AL1.cta_duplicada = 'SI' THEN 1 END) AS CtaDuplicadas,
                COUNT(CASE WHEN AL1.estado IN ('ACTIVO', 'FINIQUITADO', 'NO ENCONTRADO') OR AL1.cta_duplicada = 'SI' THEN 1 END) AS total_Usuarios
            FROM 
                dbo.ftc_agrupa_activos AL1
            JOIN dbo.ftc_pais_negocio_sistema AL2 
                ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN dbo.ftc_sistema AL3 
                ON AL3.idSistema = AL2.idSistema
            JOIN dbo.ftc_pais AL4 
                ON AL4.idPais = AL2.idPais
            JOIN dbo.ftc_negocio AL5
                ON AL5.idNegocio = AL2.idNegocio
            WHERE 
                (@idNegocio IS NULL OR AL5.idNegocio = @idNegocio)
                AND (@idSistema IS NULL OR AL3.idSistema = @idSistema)
            GROUP BY AL3.sistema, AL5.negocio, AL4.pais
        ),
        GestionDiariaData AS (
            SELECT DISTINCT 
                ftc_sistema.sistema AS Sistema,
                ftc_negocio.negocio AS Negocio,
                ftc_pais.pais AS Pais,
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
            INNER JOIN ftc_negocio
                ON ftc_pais_negocio_sistema.idNegocio = ftc_negocio.idNegocio
            WHERE 
                ftc_gestion_diaria.feccarga = (
                    SELECT TOP 1 feccarga 
                    FROM ftc_gestion_diaria 
                    ORDER BY SUBSTRING(feccarga,7,4)+SUBSTRING(feccarga,4,2)+SUBSTRING(feccarga,1,2) DESC
                )
                AND (@idNegocio IS NULL OR ftc_negocio.idNegocio = @idNegocio)
                AND (@idSistema IS NULL OR ftc_sistema.idSistema = @idSistema)
        )

        SELECT 
            A.Sistema,
            A.Negocio,
            A.Pais,
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
            ON A.Sistema = G.Sistema AND A.Negocio = G.Negocio AND A.Pais = G.Pais;
        ", new { idNegocio, idSistema });
            }
        }



        public async Task<IEnumerable<Finiquitados>> ObtenerDetalleFiniquitados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Finiquitados>(@"
            
        SELECT 
            AL4.pais, 
            AL5.negocio,
            AL3.sistema, 
            AL3.codSistema,
            AL1.idPaisNegocioSistema, 
            AL1.rutdni, 
            AL1.dv, 
            AL1.nombreusuario, 
            AL1.userid, 
            AL1.mailusuario, 
            AL1.cargospr, 
            AL1.perfil, 
            AL1.cargo,
            AL1.codcosto, 
            AL1.codccostospr, 
            AL1.Nomccostospr, 
            AL1.codccosto, 
            AL1.Nomccosto, 
            AL1.fecalta, 
            AL1.fecbaja,
            AL1.fecact, 
            AL1.fecultlogin, 
            AL1.ctasfallidas, 
            AL1.estado, 
            AL1.fecfiniq, 
            AL1.feccargafiniq, 
            AL1.cargomatriz,
            AL1.perfilmatriz, 
            AL1.feccarga, 
            AL1.empresa,
            AL1.cta_duplicada, 
            AL1.fechaad 
        FROM 
            dbo.ftc_agrupa_activos AL1
        INNER JOIN dbo.ftc_pais_negocio_sistema AL2 
            ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
        INNER JOIN dbo.ftc_sistema AL3 
            ON AL3.idSistema = AL2.idSistema
        INNER JOIN dbo.ftc_pais AL4 
            ON AL4.idPais = AL2.idPais
        INNER JOIN dbo.ftc_negocio AL5
            ON AL5.idNegocio = AL2.idNegocio
        WHERE 
            AL1.estado = 'FINIQUITADO'
            AND (@idNegocio IS NULL OR AL5.idNegocio = @idNegocio)
            AND (@idSistema IS NULL OR AL3.idSistema = @idSistema);
        ", new { idNegocio, idSistema });
            }
        }


        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerDetalleNoEncontrados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(@"
            
        SELECT 
            AL4.pais, 
            AL5.negocio,
            AL3.sistema, 
            AL3.codSistema,
            AL1.idPaisNegocioSistema, 
            AL1.rutdni, 
            AL1.dv, 
            AL1.nombreusuario, 
            AL1.userid, 
            AL1.mailusuario, 
            AL1.cargospr, 
            AL1.perfil, 
            AL1.cargo,
            AL1.codcosto, 
            AL1.codccostospr, 
            AL1.Nomccostospr, 
            AL1.codccosto, 
            AL1.Nomccosto, 
            AL1.fecalta, 
            AL1.fecbaja,
            AL1.fecact, 
            AL1.fecultlogin, 
            AL1.ctasfallidas, 
            AL1.estado, 
            AL1.fecfiniq, 
            AL1.feccargafiniq, 
            AL1.cargomatriz,
            AL1.perfilmatriz, 
            AL1.feccarga, 
            AL1.empresa,
            AL1.cta_duplicada, 
            AL1.fechaad 
        FROM 
            dbo.ftc_agrupa_activos AL1
        INNER JOIN dbo.ftc_pais_negocio_sistema AL2 
            ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
        INNER JOIN dbo.ftc_sistema AL3 
            ON AL3.idSistema = AL2.idSistema
        INNER JOIN dbo.ftc_pais AL4 
            ON AL4.idPais = AL2.idPais
        INNER JOIN dbo.ftc_negocio AL5
            ON AL5.idNegocio = AL2.idNegocio
        WHERE 
            AL1.estado = 'NO ENCONTRADO'
            AND (@idNegocio IS NULL OR AL5.idNegocio = @idNegocio)
            AND (@idSistema IS NULL OR AL3.idSistema = @idSistema);
        ", new { idNegocio, idSistema });
            }
        }


        public async Task<IEnumerable<UsuariosDuplicados>> ObtenerDetalleDuplicados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosDuplicados>(@"
            
        WITH CTE_Cuentas AS (
            SELECT 
                AL4.pais, 
                AL5.negocio AS Negocio,
                AL3.sistema, 
                AL3.codSistema, 
                AL1.idPaisNegocioSistema, 
                AL1.rutdni, 
                AL1.dv, 
                AL1.nombreusuario, 
                AL1.userid, 
                AL1.mailusuario, 
                AL1.cargospr, 
                AL1.perfil, 
                AL1.cargo,
                AL1.codcosto, 
                AL1.codccostospr, 
                AL1.Nomccostospr, 
                AL1.codccosto, 
                AL1.Nomccosto, 
                AL1.fecalta, 
                AL1.fecbaja,
                AL1.fecact, 
                AL1.fecultlogin, 
                AL1.ctasfallidas, 
                AL1.estado, 
                AL1.fecfiniq, 
                AL1.feccargafiniq, 
                AL1.cargomatriz,
                AL1.perfilmatriz, 
                AL1.feccarga, 
                AL1.empresa,
                AL1.cta_duplicada, 
                AL1.fechaad,
                ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.fechaad DESC) AS RowNum
            FROM dbo.ftc_agrupa_activos AL1
            JOIN dbo.ftc_pais_negocio_sistema AL2 
                ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN dbo.ftc_sistema AL3 
                ON AL3.idSistema = AL2.idSistema
            JOIN dbo.ftc_pais AL4 
                ON AL4.idPais = AL2.idPais
            JOIN dbo.ftc_negocio AL5
                ON AL5.idNegocio = AL2.idNegocio
            WHERE AL1.cta_duplicada = 'SI'
            AND (@idNegocio IS NULL OR AL5.idNegocio = @idNegocio)
            AND (@idSistema IS NULL OR AL3.idSistema = @idSistema)
        )
        SELECT *
        FROM CTE_Cuentas
        WHERE RowNum = 1;
        ", new { idNegocio, idSistema });
            }
        }

        public async Task<IEnumerable<Sistema>> ObtenerSistemasPorNegocio(int? idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT DISTINCT S.idSistema, S.codSistema, S.sistema
            FROM ftc_sistema S
            INNER JOIN ftc_pais_negocio_sistema PNS
                ON S.idSistema = PNS.idSistema
            WHERE (@idNegocio IS NULL OR PNS.idNegocio = @idNegocio)
            ORDER BY S.sistema";

                return await dbdapper.QueryAsync<Sistema>(query, new { idNegocio });
            }
        }


    }
}