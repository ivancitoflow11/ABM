using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioReportes
    {
        Task<IEnumerable<DifCargoPerfil>> ObtenerListaDifCargoPerfilPorSistema(int? Sistema, int idUsuarioLogueado);
        Task<IEnumerable<Finiquitados>> ObtenerListaFiniquitadosPorSistema(string Sistema, int idUsuarioLogueado);
        Task<IEnumerable<TiempoInactividad>> ObtenerListaInactividadPorSistema(string Sistema, int idUsuarioLogueado);
        Task<IEnumerable<UsuariosActivos>> ObtenerListaUsuariosActivosPorSistema(int? Sistema, int idUsuarioLogueado);
        Task<IEnumerable<UsuariosNoEncontrados>> ObtenerListaUsuariosNoEncontradosPorSistema(string Sistema, int idUsuarioLogueado);
        Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(string Sistema, int idUsuarioLogueado);
        Task<IEnumerable<UsersBuscar>> ObtenerUserPorNombreORut(string nombreusuario, string rutdni, int idUsuarioLogueado);
        Task<IEnumerable<InfoUser>> ObtenerCargoPorRut(string rutdni, int idUsuarioLogueado);
        Task<IEnumerable<UsersBuscar>> ObtenerUserPorRut(string rutdni, int idUsuarioLogueado);
    }
    public class RepositorioReportes : IRepositorioReportes
    {
        private readonly string connectionString;

        public RepositorioReportes(IConfiguration configuration)
        {
			connectionString = configuration.GetConnectionString("CadenaSQL") + ";Command Timeout=120";
		}

        public async Task<IEnumerable<TiempoInactividad>> ObtenerListaInactividadPorSistema(string Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<TiempoInactividad>(@"
            SELECT DISTINCT 
                AL4.pais, 
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
                DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) AS DiasDesdeUltLogin,
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                S.ID_Subgerencia, 
                S.Nom_Subgerencia
            FROM 
                dbo.im_agrupa_activos AL1
            JOIN 
                dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN 
                dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN 
                dbo.pais AL4 ON AL4.idPais = AL2.idPais
            LEFT JOIN 
                dbo.im_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                AL4.pais = 'IMPERIAL'
                AND AL3.sistema IN (@Sistema)
                AND DATEDIFF(DAY, AL1.fecultlogin, GETDATE()) > 30
                AND (
                    -- Si tiene gerencia, filtra por esa gerencia
                    G.ID_gerencia = (
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    )
                    OR (
                        -- Si no tiene gerencia, muestra todas las gerencias
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
                AND (
                    -- Si tiene subgerencia, filtra por esa subgerencia
                    S.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    OR (
                        -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                        S.COD_Gerencia = (
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        AND (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    OR (
                        -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                        SELECT ID_gerencia
                        FROM dbo.usuario
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                );
        ", new { Sistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<Finiquitados>> ObtenerListaFiniquitadosPorSistema(string Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Finiquitados>(@"
            SELECT DISTINCT 
                AL4.pais, 
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
                dbo.im_agrupa_activos AL1
            JOIN 
                dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN 
                dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN 
                dbo.pais AL4 ON AL4.idPais = AL2.idPais
            LEFT JOIN 
                dbo.im_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                AL4.pais = 'IMPERIAL'
                AND AL1.estado = 'FINIQUITADO'
                AND AL3.sistema = @Sistema
                AND (
                    -- Si tiene gerencia, filtra por esa gerencia
                    G.ID_gerencia = (
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    )
                    OR (
                        -- Si no tiene gerencia, muestra todas las gerencias
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
                AND (
                    -- Si tiene subgerencia, filtra por esa subgerencia
                    S.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    OR (
                        -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                        S.COD_Gerencia = (
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        AND (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    OR (
                        -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                        SELECT ID_gerencia
                        FROM dbo.usuario
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                );
        ", new { Sistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerListaUsuariosNoEncontradosPorSistema(string Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(@"
            SELECT DISTINCT 
                AL4.pais, 
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
                dbo.im_agrupa_activos AL1
            JOIN 
                dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN 
                dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN 
                dbo.pais AL4 ON AL4.idPais = AL2.idPais
            LEFT JOIN 
                dbo.im_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            LEFT JOIN 
                dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            WHERE 
                AL4.pais = 'IMPERIAL'
                AND AL1.estado = 'NO ENCONTRADO'
                AND AL3.sistema = @Sistema
                AND (
                    -- Si tiene gerencia, filtra por esa gerencia
                    G.ID_gerencia = (
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    )
                    OR (
                        -- Si no tiene gerencia, muestra todas las gerencias
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
                AND (
                    -- Si tiene subgerencia, filtra por esa subgerencia
                    S.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    OR (
                        -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                        S.COD_Gerencia = (
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        AND (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    OR (
                        -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                        SELECT ID_gerencia
                        FROM dbo.usuario
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                );
        ", new { Sistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<UsuariosActivos>> ObtenerListaUsuariosActivosPorSistema(int? Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsuariosActivos>(@"
            WITH UniqueRutDni AS (
                SELECT 
                    AL4.codPais, 
                    AL4.pais, 
                    AL2.idPaisNegocioSistema, 
                    AL3.sistema, 
                    AL2.rutdni, 
                    AL2.dv, 
                    AL2.nombreusuario,
                    AL2.userid, 
                    AL2.mailusuario, 
                    AL2.cargospr AS CargoSpr, 
                    AL2.perfil AS Perfisistema, 
                    AL2.codccostospr AS CodigoCentroCosto, 
                    AL2.Nomccostospr AS CentroCosto, 
                    AL2.fecalta, 
                    AL2.fecbaja, 
                    AL2.fecact, 
                    AL2.fecultlogin, 
                    AL2.estado AS EstadoUsuario, 
                    AL2.fecfiniq AS FechaFiniquito, 
                    AL2.cargomatriz AS CargoMatriz, 
                    AL2.perfilmatriz AS PerfilMatriz, 
                    AL2.empresa AS EmpresaRelacionada, 
                    AL2.fechaad AS UltimoAccesoAD, 
                    AL2.cta_duplicada, 
                    AL2.feccarga AS FechaCarga,
                    G.ID_gerencia, 
                    G.Nom_Gerencia, 
                    S.ID_Subgerencia, 
                    S.Nom_Subgerencia,
                    ROW_NUMBER() OVER (PARTITION BY AL2.rutdni ORDER BY AL2.fecultlogin DESC) AS rn
                FROM  
                    dbo.pais_negocio_sistema AL1
                INNER JOIN 
                    im_agrupa_activos AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                INNER JOIN 
                    dbo.sistema AL3 ON AL3.idSistema = AL1.idSistema 
                INNER JOIN 
                    dbo.pais AL4 ON AL4.idPais = AL1.idPais
                LEFT JOIN 
                    dbo.im_Subgerencias S ON AL2.Nomccostospr = S.Nom_Subgerencia
                LEFT JOIN 
                    dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
                WHERE 
                    AL2.estado <> 'NO ENCONTRADOS EN FALANET' 
                    AND AL4.idPais = 1
                    AND AL1.idPaisNegocioSistema = @Sistema
                    AND (
                        -- Si tiene gerencia, filtra por esa gerencia
                        G.ID_gerencia = (
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        OR (
                            -- Si no tiene gerencia, muestra todas las gerencias
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    AND (
                        -- Si tiene subgerencia, filtra por esa subgerencia
                        S.Nom_Subgerencia = (
                            SELECT Nom_Subgerencia
                            FROM dbo.im_Subgerencias
                            WHERE ID_Subgerencia = (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                        )
                        OR (
                            -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                            S.COD_Gerencia = (
                                SELECT ID_gerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                            AND (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            ) IS NULL
                        )
                        OR (
                            -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
            )
            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
        ", new { Sistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<DifCargoPerfil>> ObtenerListaDifCargoPerfilPorSistema(int? Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<DifCargoPerfil>(@"
            WITH UniqueRutDni AS (
                SELECT 
                    -- Columnas de im_matriz_diaria
                    AL1.idPaisNegocioSistema,
                    AL3.sistema, 
                    AL1.idCarga, 
                    AL1.rutdni, 
                    AL1.nombreusuario, 
                    AL1.userid, 
                    AL1.cargospr AS cargo, 
                    AL1.perfil, 
                    AL1.fechaEsperaba_ex, 
                    AL1.fechaAutorizacion_ex, 
                    AL1.usersSistaAdmin_ex, 
                    AL1.aprobado_ex, 
                    AL1.fechaesperada_ex,

                    -- Columnas adicionales de im_agrupa_activos y sus relaciones
                    AGR.dv, 
                    AGR.estado, 
                    AGR.codccostospr, 
                    AGR.Nomccostospr, 
                    AGR.empresa, 
                    PAIS.pais,

                    -- Columnas de gerencia y subgerencia
                    G.ID_gerencia, 
                    G.Nom_Gerencia, 
                    S.ID_Subgerencia, 
                    S.Nom_Subgerencia,

                    ROW_NUMBER() OVER (PARTITION BY CONCAT(AL1.rutdni, AL1.userid) ORDER BY AL1.idCarga DESC) AS rn
                FROM 
                    dbo.im_matriz_diaria AL1
                -- Relaciones de im_matriz_diaria
                JOIN 
                    dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                JOIN 
                    dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
                JOIN 
                    dbo.pnsjt AL4 ON AL2.idPaisNegocioSistema = AL4.idPaisNegocioSistema
                    AND AL1.estado_ex = 'PENDIENTE'
                LEFT JOIN 
                    (SELECT 
                        rutdni, 
                        nombreusuario, 
                        idPaisNegocioSistema, 
                        MAX(Nomccostospr) AS Nomccostospr
                     FROM 
                        dbo.im_agrupa_activos 
                     GROUP BY 
                        rutdni, nombreusuario, idPaisNegocioSistema) AA 
                    ON AL1.rutdni = AA.rutdni 
                    AND AL1.nombreusuario = AA.nombreusuario 
                    AND AL1.idPaisNegocioSistema = AA.idPaisNegocioSistema 

                -- Relaciones adicionales con im_agrupa_activos
                LEFT JOIN 
                    dbo.im_agrupa_activos AGR ON AL1.rutdni = AGR.rutdni 
                    AND AL1.idPaisNegocioSistema = AGR.idPaisNegocioSistema
                    AND AGR.cargomatriz IS NULL
                    AND AGR.estado = 'ACTIVO'
                LEFT JOIN 
                    dbo.comentarios COM ON AGR.llave_ex = COM.llave_ex 
                    AND COM.llave_ex IS NULL
                LEFT JOIN 
                    dbo.pais_negocio_sistema PNS ON PNS.idPaisNegocioSistema = AGR.idPaisNegocioSistema
                LEFT JOIN 
                    dbo.pais PAIS ON PAIS.idPais = PNS.idPais
                LEFT JOIN 
                    dbo.im_Subgerencias S ON AGR.Nomccostospr = S.Nom_Subgerencia
                LEFT JOIN 
                    dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia

                WHERE 
                    -- Condiciones de im_matriz_diaria
                    AL1.estado_ex <> 'CERRADO' 
                    AND AL1.idPaisNegocioSistema = @Sistema
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                    AND AL1.fechaAutorizacion_ex IS NULL

                    -- Condición adicional de im_agrupa_activos
                    AND PAIS.idPais = 1

                    -- Filtrado por gerencia y subgerencia
                    AND (
                        -- Si tiene gerencia, filtra por gerencia
                        G.ID_gerencia = (
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        OR (
                            -- Si no tiene gerencia, muestra todas las gerencias
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    AND (
                        -- Si tiene subgerencia, filtra por subgerencia
                        S.Nom_Subgerencia = (
                            SELECT Nom_Subgerencia
                            FROM dbo.im_Subgerencias
                            WHERE ID_Subgerencia = (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                        )
                        OR (
                            -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                            S.COD_Gerencia = (
                                SELECT ID_gerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                            AND (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            ) IS NULL
                        )
                        OR (
                            -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
            )
            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
        ", new { Sistema, idUsuarioLogueado });
            }
        }



        public async Task<IEnumerable<UltimaConexion>> ObtenerListaUltimaConexion(string Sistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UltimaConexion>(@"
            SELECT DISTINCT 
    a.rutdni, 
    a.dv, 
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
FROM im_agrupa_activos a
JOIN pais_negocio_sistema b ON a.idPaisNegocioSistema = b.idPaisNegocioSistema
JOIN pais c ON b.idPais = c.idPais
JOIN negocio d ON b.idNegocio = d.idNegocio
JOIN sistema e ON b.idSistema = e.idSistema
LEFT JOIN dbo.im_Subgerencias s ON a.Nomccostospr = s.Nom_Subgerencia
LEFT JOIN dbo.im_gerencia g ON s.COD_Gerencia = g.ID_gerencia
WHERE 
    c.idPais = 1
    AND a.fechaad IS NULL
    AND e.sistema = @Sistema
    AND (
        -- Si el usuario tiene gerencia, filtra por esa gerencia
        g.ID_gerencia = (
            SELECT ID_gerencia 
            FROM dbo.usuario 
            WHERE IdUsuario = @idUsuarioLogueado
        )
        OR (
            -- Si el usuario no tiene gerencia, muestra todas las gerencias
            SELECT ID_gerencia 
            FROM dbo.usuario 
            WHERE IdUsuario = @idUsuarioLogueado
        ) IS NULL
    )
    AND (
        -- Si el usuario tiene subgerencia, filtra por esa subgerencia
        s.Nom_Subgerencia = (
            SELECT Nom_Subgerencia
            FROM dbo.im_Subgerencias
            WHERE ID_Subgerencia = (
                SELECT ID_Subgerencia
                FROM dbo.usuario
                WHERE IdUsuario = @idUsuarioLogueado
            )
        )
        -- Si el usuario NO tiene subgerencia, muestra todos los registros
        OR (
            SELECT ID_Subgerencia
            FROM dbo.usuario
            WHERE IdUsuario = @idUsuarioLogueado
        ) IS NULL
    );
",
                    new { Sistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<UsersBuscar>> ObtenerUserPorNombreORut(string nombreusuario, string rutdni, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsersBuscar>(@"
            WITH UniqueRutDni AS (
                SELECT DISTINCT 
                    dbo.negocio.negocio, 
                    dbo.sistema.sistema, 
                    a.rutdni, 
                    a.nombreusuario, 
                    a.cargospr, 
                    a.perfil, 
                    a.fecultlogin, 
                    a.cargomatriz,
                    a.perfilmatriz, 
                    a.estado, 
                    a.fecfiniq, 
                    a.fechaad, 
                    a.Nomccostospr,
                    a.codccostospr, 
                    a.userid, 
                    a.Empresa, 
                    a.codccosto,
                    G.ID_gerencia, 
                    G.Nom_Gerencia, 
                    S.ID_Subgerencia, 
                    S.Nom_Subgerencia,
                    ROW_NUMBER() OVER (PARTITION BY a.rutdni ORDER BY a.fecultlogin DESC) AS rn
                FROM 
                    dbo.negocio 
                INNER JOIN 
                    dbo.pais_negocio_sistema ON dbo.negocio.idNegocio = dbo.pais_negocio_sistema.idNegocio 
                INNER JOIN 
                    dbo.pais ON dbo.pais_negocio_sistema.idPais = dbo.pais.idPais 
                INNER JOIN 
                    dbo.sistema ON dbo.pais_negocio_sistema.idSistema = dbo.sistema.idSistema 
                INNER JOIN 
                    im_agrupa_activos a ON dbo.pais_negocio_sistema.idPaisNegocioSistema = a.idPaisNegocioSistema 
                LEFT JOIN 
                    im_matriz_diaria md ON a.llave_ex = md.llave_ex
                LEFT JOIN 
                    dbo.im_Subgerencias S ON a.Nomccostospr = S.Nom_Subgerencia
                LEFT JOIN 
                    dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
                WHERE 
                    dbo.sistema.sistema = 'ADAPI' -- Filtrar por sistema ADAPI
                    AND (@rutdni IS NULL OR a.rutdni LIKE '%' + @rutdni + '%')
                    AND (@nombreusuario IS NULL OR a.nombreusuario LIKE '%' + @nombreusuario + '%')
                    AND (
                        -- Si tiene gerencia, filtra por esa gerencia
                        G.ID_gerencia = (
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        OR (
                            -- Si no tiene gerencia, muestra todas las gerencias
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    AND (
                        -- Si tiene subgerencia, filtra por esa subgerencia
                        S.Nom_Subgerencia = (
                            SELECT Nom_Subgerencia
                            FROM dbo.im_Subgerencias
                            WHERE ID_Subgerencia = (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                        )
                        OR (
                            -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                            S.COD_Gerencia = (
                                SELECT ID_gerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            )
                            AND (
                                SELECT ID_Subgerencia
                                FROM dbo.usuario
                                WHERE IdUsuario = @idUsuarioLogueado
                            ) IS NULL
                        )
                        OR (
                            -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
            )

            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
        ", new { nombreusuario, rutdni, idUsuarioLogueado });
            }
        }


        public async Task<IEnumerable<UsersBuscar>> ObtenerUserPorRut(string rutdni, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<UsersBuscar>(@"
            SELECT DISTINCT 
                a.rutdni, 
                a.dv, 
                a.nombreusuario, 
                a.userid, 
                a.cargospr, 
                a.codccostospr, 
                a.Nomccosto, 
                s.sistema,
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                SG.ID_Subgerencia, 
                SG.Nom_Subgerencia
            FROM 
                im_agrupa_activos a
            LEFT JOIN 
                pais_negocio_sistema p ON a.idPaisNegocioSistema = p.idPaisNegocioSistema
            LEFT JOIN 
                sistema s ON p.idSistema = s.idSistema
            LEFT JOIN 
                dbo.im_Subgerencias SG ON a.Nomccostospr = SG.Nom_Subgerencia
            LEFT JOIN 
                dbo.im_gerencia G ON SG.COD_Gerencia = G.ID_gerencia
            WHERE 
                a.rutdni = @rutdni
                AND s.sistema = 'ADAPI' -- Filtrar por sistema ADAPI
                AND (
                    -- Si tiene gerencia, filtra por esa gerencia
                    G.ID_gerencia = (
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    )
                    OR (
                        -- Si no tiene gerencia, muestra todas las gerencias
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
                AND (
                    -- Si tiene subgerencia, filtra por esa subgerencia
                    SG.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    OR (
                        -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                        SG.COD_Gerencia = (
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        AND (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    OR (
                        -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                        SELECT ID_gerencia
                        FROM dbo.usuario
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                );
        ", new { rutdni, idUsuarioLogueado });
            }
        }



        public async Task<IEnumerable<InfoUser>> ObtenerCargoPorRut(string rutdni, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<InfoUser>(@"
            SELECT DISTINCT 
                'ADAPI' AS Sistema, -- Sistema establecido como ADAPI
                mp.Cargo, 
                mp.Perfil, 
                G.ID_gerencia, 
                G.Nom_Gerencia, 
                SG.ID_Subgerencia, 
                SG.Nom_Subgerencia
            FROM 
                dbo.im_sodimac_matriz_perfil mp
            INNER JOIN 
                dbo.im_agrupa_activos aa ON mp.Cargo = aa.Cargo
            LEFT JOIN 
                dbo.im_Subgerencias SG ON aa.Nomccostospr = SG.Nom_Subgerencia
            LEFT JOIN 
                dbo.im_gerencia G ON SG.COD_Gerencia = G.ID_gerencia
            WHERE 
                aa.rutdni = @rutdni
                AND (
                    -- Si tiene gerencia, filtra por esa gerencia
                    G.ID_gerencia = (
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    )
                    OR (
                        -- Si no tiene gerencia, muestra todas las gerencias
                        SELECT ID_gerencia 
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
                AND (
                    -- Si tiene subgerencia, filtra por esa subgerencia
                    SG.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    OR (
                        -- Si no tiene subgerencia pero tiene gerencia, muestra todas las subgerencias de su gerencia
                        SG.COD_Gerencia = (
                            SELECT ID_gerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        AND (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        ) IS NULL
                    )
                    OR (
                        -- Si no tiene subgerencia ni gerencia, muestra todos los registros
                        SELECT ID_gerencia
                        FROM dbo.usuario
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                );
        ", new { rutdni, idUsuarioLogueado });
            }
        }



    }
}
