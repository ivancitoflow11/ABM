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
                    // Consulta SQL adaptada para ser consistente con los otros reportes
                    var query = @"
                SELECT DISTINCT 
                    AL4.pais,
                    AL5.negocio,
                    AL3.sistema, 
                    AL1.rutdni, 
                    AL1.dv, 
                    AL1.nombreusuario, 
                    AL1.userid, 
                    AL1.fecultlogin, 
                    AL1.estado,
                    DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) AS DiasDesdeUltLogin
                FROM 
                    dbo.ftc_agrupa_activos AL1
                JOIN 
                    dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                JOIN 
                    dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema
                JOIN 
                    dbo.ftc_pais AL4 ON AL4.idPais = AL2.idPais
                JOIN
                    dbo.ftc_negocio AL5 ON AL5.idNegocio = AL2.idNegocio
                WHERE 
                    DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) > 30
                    AND AL2.idPais = @idpais
                    AND AL2.idNegocio = @idnegocio;
            ";

                    return await dbdapper.QueryAsync<TiempoInactividad>(query, new { idpais, idnegocio });
                }
            }
        

        public async Task<IEnumerable<DifCargoPerfil>> ObtenerListaDifCargoPerfil(int idpais, int idnegocio)
        {
            // Se ha eliminado la lógica de permisos de usuario.
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Consulta SQL simplificada sin joins ni filtros de gerencia/subgerencia.
                var query = @"
            WITH UniqueRutDni AS (
                SELECT 
                    AL1.rutdni, 
                    AGR.dv,
                    AL1.nombreusuario, 
                    AL1.userid,
                    PAIS.pais,
                    AL3.sistema, 
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
                JOIN dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                JOIN dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema
                LEFT JOIN dbo.ftc_agrupa_activos AGR ON AL1.rutdni = AGR.rutdni 
                    AND AL1.idPaisNegocioSistema = AGR.idPaisNegocioSistema AND AGR.cargomatriz IS NULL AND AGR.estado = 'ACTIVO'
                LEFT JOIN dbo.ftc_pais PAIS ON PAIS.idPais = AL2.idPais
                WHERE 
                    AL1.estado_ex <> 'CERRADO' 
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND AL2.idPais = @idpais
                    AND AL2.idNegocio = @idnegocio
            )
            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
        ";

                return await dbdapper.QueryAsync<DifCargoPerfil>(
                    query,
                    new { idpais, idnegocio }
                );
            }
        }
        public async Task<IEnumerable<Finiquitados>> ObtenerListaFiniquitadosPorSistema(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT DISTINCT 
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
    AL1.fechaad,
    G.ID_gerencia, 
    G.Nom_Gerencia, 
    S.ID_Subgerencia, 
    S.Nom_Subgerencia
FROM 
    dbo.ftc_agrupa_activos AL1
JOIN 
    dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
JOIN 
    dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema
JOIN 
    dbo.ftc_pais AL4 ON AL4.idPais = AL2.idPais
JOIN
	dbo.ftc_negocio AL5 ON AL5.idNegocio = AL2.idNegocio
LEFT JOIN 
    dbo.ftc_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
LEFT JOIN 
    dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
WHERE 
    AL2.idPais = @idpais
    AND AL1.estado = 'FINIQUITADO'
    AND AL2.idNegocio = @idnegocio";

                return await dbdapper.QueryAsync<Finiquitados>(query, new { idpais, idnegocio });
            }
        }





        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerListaUsuariosNoEncontrados(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT DISTINCT 
                AL4.pais, 
                AL3.sistema, 
				AL5.negocio,
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
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                S.ID_Subgerencia, 
                S.Nom_Subgerencia
            FROM 
                dbo.ftc_agrupa_activos AL1
            JOIN 
                dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN 
                dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN 
                dbo.ftc_pais AL4 ON AL4.idPais = AL2.idPais
			JOIN
				dbo.ftc_negocio AL5 ON AL5.idNegocio = AL2.idNegocio
            LEFT JOIN 
                dbo.ftc_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                AL1.estado = 'NO ENCONTRADO'
				AND AL2.idPais = @idpais
				AND AL2.idNegocio = @idnegocio";

                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(query, new { idpais, idnegocio });
            }
        }

        public async Task<IEnumerable<UsersBuscar>> ObtenerUsuariosPorRutONombre(int idPais, int idNegocio, string rutDni = null, string nombreUsuario = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
SELECT DISTINCT
    a.rutdni,
    a.dv,
    c.pais,
    d.negocio,
    st.sistema,
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
FROM dbo.ftc_agrupa_activos           AS a
JOIN dbo.ftc_pais_negocio_sistema    AS b ON b.idPaisNegocioSistema = a.idPaisNegocioSistema
JOIN dbo.ftc_pais                     AS c ON c.idPais                 = b.idPais
JOIN dbo.ftc_negocio                  AS d ON d.idNegocio              = b.idNegocio
JOIN dbo.ftc_sistema                  AS st ON st.idSistema              = b.idSistema
LEFT JOIN dbo.ftc_Subgerencias        AS s ON s.Nom_Subgerencia        = a.Nomccostospr
LEFT JOIN dbo.ftc_gerencia            AS g ON g.ID_gerencia            = s.COD_Gerencia
WHERE
    b.idPais     = @idPais
    AND b.idNegocio = @idNegocio
    AND (@rutDni        IS NULL OR a.rutdni       LIKE '%' + @rutDni + '%')
    AND (@nombreUsuario IS NULL OR a.nombreusuario LIKE '%' + @nombreUsuario + '%');";

                return await db.QueryAsync<UsersBuscar>(sql, new { idPais, idNegocio, rutDni, nombreUsuario });
            }
        }



        public async Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(int idpais, int idnegocio)
        {
            // Se ha eliminado toda la lógica para obtener el usuario logueado y sus permisos.
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Consulta SQL simplificada sin los filtros de gerencia/subgerencia.
                var query = @"
                SELECT DISTINCT 
                    a.rutdni, 
                    a.dv, 
                    c.pais,
                    d.negocio,
                    e.sistema, 
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
                JOIN dbo.ftc_pais_negocio_sistema b ON a.idPaisNegocioSistema = b.idPaisNegocioSistema
                JOIN dbo.ftc_pais c ON b.idPais = c.idPais
                JOIN dbo.ftc_negocio d ON b.idNegocio = d.idNegocio
                JOIN dbo.ftc_sistema e ON b.idSistema = e.idSistema
                LEFT JOIN dbo.ftc_Subgerencias s ON a.Nomccostospr = s.Nom_Subgerencia
                LEFT JOIN dbo.ftc_gerencia g ON s.COD_Gerencia = g.ID_gerencia
                WHERE 
                    b.idPais = @idpais
                    AND b.idNegocio = @idnegocio
                    AND a.fechaad IS NULL";

                // Los únicos parámetros necesarios ahora son idpais e idnegocio.
                return await dbdapper.QueryAsync<UltimaConexion>(
                    query,
                    new { idpais, idnegocio }
                );
            }
        }
        public async Task<IEnumerable<UsuariosActivos>> ObtenerListaUsuariosActivos(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
SELECT DISTINCT
    AL4.pais,
    AL3.sistema,
    AL5.negocio,
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
FROM dbo.ftc_agrupa_activos           AS AL1
JOIN dbo.ftc_pais_negocio_sistema    AS AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
JOIN dbo.ftc_sistema                  AS AL3 ON AL3.idSistema               = AL2.idSistema
JOIN dbo.ftc_pais                     AS AL4 ON AL4.idPais                  = AL2.idPais
JOIN dbo.ftc_negocio                  AS AL5 ON AL5.idNegocio               = AL2.idNegocio
LEFT JOIN dbo.ftc_Subgerencias        AS S   ON AL1.Nomccostospr            = S.Nom_Subgerencia
LEFT JOIN dbo.ftc_gerencia            AS G   ON S.COD_Gerencia              = G.ID_gerencia
WHERE
    AL1.estado = 'ACTIVO'
    AND AL2.idPais    = @idpais
    AND AL2.idNegocio = @idnegocio;
";

                return await dbdapper.QueryAsync<UsuariosActivos>(query, new { idpais, idnegocio });
            }
        }

    }
}
