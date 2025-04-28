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

        public async Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT DISTINCT 
    a.rutdni, 
    a.dv, 
	c.pais,
	d.negocio,
    a.nombreusuario, 
    a.estado, 
    a.fecfiniq AS fecha_finiquito, 
    a.fecultlogin AS ultima_conexion,  
    a.fechaad AS Fecha_AD, 
    e.sistema, 
    a.idPaisNegocioSistema,
    g.ID_gerencia, 
    g.Nom_Gerencia, 
    s.ID_Subgerencia, 
    s.Nom_Subgerencia
FROM ftc_agrupa_activos a
JOIN ftc_pais_negocio_sistema b ON a.idPaisNegocioSistema = b.idPaisNegocioSistema
JOIN ftc_pais c ON b.idPais = c.idPais
JOIN ftc_negocio d ON b.idNegocio = d.idNegocio
JOIN ftc_sistema e ON b.idSistema = e.idSistema
LEFT JOIN dbo.ftc_Subgerencias s ON a.Nomccostospr = s.Nom_Subgerencia
LEFT JOIN dbo.ftc_gerencia g ON s.COD_Gerencia = g.ID_gerencia
WHERE 
    a.fechaad IS NULL
	AND b.idPais = @idpais
	AND b.idNegocio = @idnegocio";

                return await dbdapper.QueryAsync<UltimaConexion>(query, new { idpais, idnegocio });
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
    AL1.estado    <> 'NO ENCONTRADO'
    AND AL2.idPais    = @idpais
    AND AL2.idNegocio = @idnegocio;
";

                return await dbdapper.QueryAsync<UsuariosActivos>(query, new { idpais, idnegocio });
            }
        }

    }
}
