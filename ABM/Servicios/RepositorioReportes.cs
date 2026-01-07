using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
    public interface IRepositorioReportes
    {
        Task<IEnumerable<Finiquitados>> ObtenerListaFiniquitadosPorSistema(int idpais, int idnegocio);
        Task<IEnumerable<UsuariosNoEncontrados>> ObtenerListaUsuariosNoEncontrados(int idpais, int idnegocio);
        Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(int idpais, int idnegocio);
        Task<IEnumerable<UsuariosActivos>> ObtenerListaUsuariosActivos(int idpais, int idnegocio);
        Task<IEnumerable<UsersBuscar>> ObtenerUsuariosPorRutONombre(int idPais, int idNegocio, string rutDni = null, string nombreUsuario = null);
        Task<IEnumerable<DifCargoPerfil>> ObtenerListaDifCargoPerfil(int idpais, int idnegocio);
        Task<IEnumerable<TiempoInactividad>> ObtenerListaTiempoInactividad(int idpais, int idnegocio);
    }

    public class RepositorioReportes : IRepositorioReportes
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioReportes(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<IEnumerable<TiempoInactividad>> ObtenerListaTiempoInactividad(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2 (Sistemas activos)
                var query = @"
                SELECT DISTINCT 
                    PNS.pais,
                    PNS.negocio,
                    PNS.sistema, 
                    AL1.rutdni, 
                    AL1.dv, 
                    AL1.nombreusuario, 
                    AL1.userid, 
                    AL1.fecultlogin, 
                    AL1.estado,
                    DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) AS DiasDesdeUltLogin
                FROM 
                    dbo.ftc_agrupa_activos AL1
                INNER JOIN 
                    dbo.ftc_pns_2 PNS ON PNS.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                WHERE 
                    DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) > 30
                    AND PNS.idPais = @idpais
                    AND PNS.idNegocio = @idnegocio;
            ";

                return await dbdapper.QueryAsync<TiempoInactividad>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<DifCargoPerfil>> ObtenerListaDifCargoPerfil(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var query = @"
            WITH UniqueRutDni AS (
                SELECT 
                    AL1.rutdni, 
                    AGR.dv,
                    AL1.nombreusuario, 
                    AL1.userid,
                    PNS.pais,
                    PNS.sistema, 
                    AL1.cargospr AS cargo, 
                    AL1.perfil, 
                    AGR.estado,
                    AGR.empresa,
                    AGR.Nomccostospr,
                    AGR.codccostospr,
                    AL1.idCarga,
                    ROW_NUMBER() OVER (PARTITION BY CONCAT(AL1.rutdni, AL1.userid) ORDER BY AL1.idCarga DESC) AS rn
                FROM 
                    dbo.ftc_matriz_diaria AL1
                INNER JOIN dbo.ftc_pns_2 PNS ON PNS.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                LEFT JOIN dbo.ftc_agrupa_activos AGR ON AL1.rutdni = AGR.rutdni 
                    AND AL1.idPaisNegocioSistema = AGR.idPaisNegocioSistema AND AGR.cargomatriz IS NULL AND AGR.estado = 'ACTIVO'
                WHERE 
                    AL1.estado_ex <> 'CERRADO' 
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND PNS.idPais = @idpais
                    AND PNS.idNegocio = @idnegocio
            )
            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
        ";

                return await dbdapper.QueryAsync<DifCargoPerfil>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<Finiquitados>> ObtenerListaFiniquitadosPorSistema(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var query = @"
            SELECT DISTINCT 
                PNS.pais, 
                PNS.negocio,
                PNS.sistema, 
                PNS.codSistema,
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
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                S.ID_Subgerencia, 
                S.Nom_Subgerencia
            FROM 
                dbo.ftc_agrupa_activos AL1
            INNER JOIN 
                dbo.ftc_pns_2 PNS ON PNS.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            LEFT JOIN 
                dbo.ftc_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                PNS.idPais = @idpais
                AND AL1.estado = 'FINIQUITADO'
                AND PNS.idNegocio = @idnegocio";

                return await dbdapper.QueryAsync<Finiquitados>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerListaUsuariosNoEncontrados(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var query = @"
            SELECT DISTINCT 
                PNS.pais, 
                PNS.sistema, 
                PNS.negocio,
                PNS.codSistema,
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
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                S.ID_Subgerencia, 
                S.Nom_Subgerencia
            FROM 
                dbo.ftc_agrupa_activos AL1
            INNER JOIN 
                dbo.ftc_pns_2 PNS ON PNS.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            LEFT JOIN 
                dbo.ftc_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                AL1.estado = 'NO ENCONTRADO'
                AND PNS.idPais = @idpais
                AND PNS.idNegocio = @idnegocio";

                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<UsersBuscar>> ObtenerUsuariosPorRutONombre(int idPais, int idNegocio, string rutDni = null, string nombreUsuario = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var sql = @"
            SELECT DISTINCT
                a.rutdni,
                a.dv,
                pns.pais,
                pns.negocio,
                pns.sistema,
                a.nombreusuario,
                a.cargospr,
                a.cargo,
                a.perfil,
                a.fecultlogin   AS fecultlogin,
                a.cargomatriz,
                a.perfilmatriz,
                a.estado,
                a.fecfiniq      AS fecfiniq,
                a.fechaad       AS fechaad,
                a.Nomccostospr,
                a.codccostospr,
                a.codccosto,
                a.userid,
                a.empresa,
                a.idPaisNegocioSistema,
                g.ID_gerencia,
                g.Nom_Gerencia,
                s.ID_Subgerencia,
                s.Nom_Subgerencia,
                ROW_NUMBER() OVER (PARTITION BY a.rutdni ORDER BY a.fecultlogin DESC) AS rn
            FROM dbo.ftc_agrupa_activos            AS a
            INNER JOIN dbo.ftc_pns_2               AS pns ON pns.idPaisNegocioSistema = a.idPaisNegocioSistema
            LEFT JOIN dbo.ftc_Subgerencias         AS s ON s.Nom_Subgerencia        = a.Nomccostospr
            LEFT JOIN dbo.ftc_gerencia             AS g ON g.ID_gerencia            = s.COD_Gerencia
            WHERE
                pns.idPais      = @idPais
                AND pns.idNegocio = @idNegocio
                AND (@rutDni        IS NULL OR a.rutdni       LIKE '%' + @rutDni + '%')
                AND (@nombreUsuario IS NULL OR a.nombreusuario LIKE '%' + @nombreUsuario + '%');";

                return await db.QueryAsync<UsersBuscar>(sql, new { idPais, idNegocio, rutDni, nombreUsuario });
            }
        }

        public async Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var query = @"
            SELECT DISTINCT 
                a.rutdni, 
                a.dv, 
                pns.pais,
                pns.negocio,
                pns.sistema, 
                a.nombreusuario, 
                a.estado, 
                a.fecfiniq AS fecha_finiquito, 
                a.fecultlogin AS ultima_conexion,  
                a.fechaad AS Fecha_AD, 
                a.idPaisNegocioSistema,
                g.ID_gerencia, 
                g.Nom_Gerencia, 
                s.ID_Subgerencia, 
                s.Nom_Subgerencia
            FROM dbo.ftc_agrupa_activos a
            INNER JOIN dbo.ftc_pns_2 pns ON a.idPaisNegocioSistema = pns.idPaisNegocioSistema
            LEFT JOIN dbo.ftc_Subgerencias s ON a.Nomccostospr = s.Nom_Subgerencia
            LEFT JOIN dbo.ftc_gerencia g ON s.COD_Gerencia = g.ID_gerencia
            WHERE 
                pns.idPais = @idpais
                AND pns.idNegocio = @idnegocio
                AND a.fechaad IS NULL";

                return await dbdapper.QueryAsync<UltimaConexion>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<UsuariosActivos>> ObtenerListaUsuariosActivos(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Cruce con ftc_pns_2
                var query = @"
            SELECT DISTINCT
                PNS.pais,
                PNS.sistema,
                PNS.negocio,
                PNS.codSistema,
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
                G.ID_gerencia,
                G.Nom_Gerencia,
                S.ID_Subgerencia,
                S.Nom_Subgerencia
            FROM dbo.ftc_agrupa_activos            AS AL1
            INNER JOIN dbo.ftc_pns_2               AS PNS ON PNS.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            LEFT JOIN dbo.ftc_Subgerencias         AS S   ON AL1.Nomccostospr         = S.Nom_Subgerencia
            LEFT JOIN dbo.ftc_gerencia             AS G   ON S.COD_Gerencia           = G.ID_gerencia
            WHERE
                AL1.estado = 'ACTIVO'
                AND PNS.idPais    = @idpais
                AND PNS.idNegocio = @idnegocio;
            ";

                return await dbdapper.QueryAsync<UsuariosActivos>(query, new { idpais, idnegocio });
            }
        }
    }
}