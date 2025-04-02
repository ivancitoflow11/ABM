using Dapper;
using ABM.Data;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioExcepciones
    {
        Task GuardarComentariosExcepcion(Comentarios excepcion);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int IdSistema);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int IdSistema, int idUsuarioLogueado);
        Task<List<Comentarios>> ObtenerListaHistoricaExcepciones();
        Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion();
        Task<IEnumerable<TipoExcepcion>> ObtenerTiposExcepciones();
        Task<TipoExcepcion> ObtenerTiposExecepcionesPorId(int Id);
        Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado);
        Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma);
    }
    public class RepositorioExcepciones : IRepositorioExcepciones
    {

        private string connectionString;

        public RepositorioExcepciones(IConfiguration configuration)
        {
			connectionString = configuration.GetConnectionString("CadenaSQL") + ";Command Timeout=120";
		}

        public async Task<IEnumerable<TipoExcepcion>> ObtenerTiposExcepciones()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<TipoExcepcion>(@"SELECT idMotivo,
                     motivo as nombreMotivo
                FROM motivo");
            }
        }

        public async Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<SistemaExcepcion>(@"SELECT AL3.sistema, 
                AL1.idPaisNegocioSistema, 
                Count (AL1.rutdni) as qty
                FROM dbo.im_matriz_diaria AL1, 
                dbo.pais_negocio_sistema AL2,
                dbo.sistema AL3, dbo.pnsjt AL4
                WHERE (AL2.idPaisNegocioSistema=AL1.idPaisNegocioSistema
                AND AL3.idSistema=AL2.idSistema 
                AND AL2.idPaisNegocioSistema=AL4.idPaisNegocioSistema)  
                AND ((
				--AL4.infomatrizperfil='NO' 
                AL1.estado_ex<>'CERRADO' 
                AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                AND AL1.fechaAutorizacion_ex IS NULL
				))
                GROUP BY AL3.sistema, AL1.idPaisNegocioSistema ORDER BY  2");


            }
        }


        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int IdSistema, int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {

                return await dbdapper.QueryAsync<DetalleExcepcion>(@"WITH UniqueRutDni AS (
                SELECT 
                    AL1.idPaisNegocioSistema,
                    AL3.sistema, 
                    AL1.idCarga, 
                    AL1.rutdni, 
                    AL1.nombreusuario, 
                    AL1.userid, 
                    AL1.cargospr, 
                    AL1.perfil, 
                    AL1.fechaEsperaba_ex, 
                    AL1.fechaAutorizacion_ex, 
                    AL1.usersSistaAdmin_ex, 
                    AL1.aprobado_ex, 
                    AL1.fechaesperada_ex,
                    G.ID_gerencia, 
                    G.Nom_Gerencia, 
                    S.ID_Subgerencia,
                    S.Nom_Subgerencia,  
                    ROW_NUMBER() OVER (PARTITION BY Concat(AL1.rutdni, userid) ORDER BY AL1.idCarga DESC) AS rn
                FROM 
                    dbo.im_matriz_diaria AL1
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
                ON 
                    AL1.rutdni = AA.rutdni 
                    AND AL1.nombreusuario = AA.nombreusuario 
                    AND AL1.idPaisNegocioSistema = AA.idPaisNegocioSistema
                JOIN 
                    dbo.im_Subgerencias S ON AA.Nomccostospr = S.Nom_Subgerencia  
                JOIN 
                    dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia  
                LEFT JOIN 
                    dbo.usuario U ON U.ID_gerencia = G.ID_gerencia
                    AND U.idUsuario = @idUsuarioLogueado
                WHERE 
                    AL1.estado_ex <> 'CERRADO' 
                    AND AL1.idPaisNegocioSistema = @IdSistema
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                    AND AL1.fechaAutorizacion_ex IS NULL
                 AND AL1.perfil IS NOT NULL
                    AND (
                        G.ID_gerencia = (
                            SELECT ID_gerencia 
                            FROM dbo.usuario 
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                        OR S.ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    AND (
                        -- Si el usuario tiene subgerencia, filtra por esa subgerencia
                        S.Nom_Subgerencia = (
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
                    )
            )
            SELECT *
            FROM UniqueRutDni
            WHERE rn = 1;
                ", new { IdSistema, idUsuarioLogueado });
            }
        }

        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int IdSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {

                return await dbdapper.QueryAsync<DetalleExcepcion>(@"WITH UniqueRutDni AS (
                    SELECT 
                        AL1.idPaisNegocioSistema,
                        AL3.sistema, 
                        AL1.idCarga, 
                        AL1.rutdni, 
                        AL1.nombreusuario, 
                        AL1.userid, 
                        AL1.cargospr, 
                        AL1.perfil, 
                        AL1.fechaEsperaba_ex, 
                        AL1.fechaAutorizacion_ex, 
                        AL1.usersSistaAdmin_ex, 
                        AL1.aprobado_ex, 
                        AL1.fechaesperada_ex,
                        G.ID_gerencia, 
                        G.Nom_Gerencia, 
                        S.ID_Subgerencia,
                        S.Nom_Subgerencia,
                        ROW_NUMBER() OVER (PARTITION BY Concat(AL1.rutdni, userid) ORDER BY AL1.idCarga DESC) AS rn
                    FROM 
                        dbo.im_matriz_diaria AL1
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
                            MAX(Nomccostospr) AS Nomccostospr  -- Usamos MAX para evitar duplicados
                         FROM 
                            dbo.im_agrupa_activos 
                         GROUP BY 
                            rutdni, nombreusuario, idPaisNegocioSistema) AA 
                    ON 
                        AL1.rutdni = AA.rutdni 
                        AND AL1.nombreusuario = AA.nombreusuario 
                        AND AL1.idPaisNegocioSistema = AA.idPaisNegocioSistema
                    LEFT JOIN 
                        dbo.im_Subgerencias S ON AA.Nomccostospr = S.Nom_Subgerencia  -- Relacionamos con Subgerencia
                    LEFT JOIN 
                        dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia  -- Relacionamos con Gerencia
                    WHERE 
                        AL1.estado_ex <> 'CERRADO' 
                        AND AL1.idPaisNegocioSistema = @IdSistema
                        AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO') 
                        AND AL1.fechaAutorizacion_ex IS NULL
                        AND AL1.perfil IS NOT NULL
                )
                SELECT *
                FROM UniqueRutDni
                WHERE rn = 1;", new { IdSistema });
            }
        }

        public async Task<TipoExcepcion> ObtenerTiposExecepcionesPorId(int Id)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<TipoExcepcion>(@"SELECT idMotivo,
                     motivo as nombreMotivo
                FROM motivo WHERE idMotivo = @Id", new { Id });
            }
        }


        public async Task GuardarComentariosExcepcion(Comentarios excepcion)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                await dbdapper.ExecuteAsync(@"INSERT INTO comentarios
                           (idCarga
                           ,idUsuario
                           ,idRol
                           ,idMotivo
                           ,estado
                           ,comentario
                           ,evidencia
                           ,fecha_autorizacion
                           ,fecha_creacion
                           ,llave_ex)
                     VALUES
                           (@idCarga
                           ,@idUsuario
                           ,@idRol
                           ,@idMotivo
                           ,@estado
                           ,@comentario
                           ,@evidencia
                           ,@fecha_autorizacion
                           ,@fecha_creacion
                           ,@llave_ex)", excepcion);
            }
        }


        public async Task<List<Comentarios>> ObtenerListaHistoricaExcepciones()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var lista = await dbdapper.QueryAsync<Comentarios>(@"SELECT 
    C.[idCarga],
    C.[idUsuario],
    C.[idRol],
    U.[nombre] AS Responsable,
    M.[motivo],
    M.[idMotivo],
    C.[estado],
    C.[comentario],
    C.[evidencia],
    C.[fecha_autorizacion],
    C.[fecha_creacion],
    C.[llave_ex],
    MD.[nombreusuario],
    MD.[perfil],
    MD.[cargospr],
    G.[Nom_Gerencia]
FROM 
    comentarios C
LEFT JOIN 
    motivo M ON C.idMotivo = M.idMotivo
LEFT JOIN 
    im_matriz_diaria MD ON C.idCarga = MD.idCarga
LEFT JOIN 
    usuario U ON C.[idUsuario] = U.idUsuario
LEFT JOIN 
    im_gerencia G ON U.ID_gerencia = G.ID_gerencia
ORDER BY 
    C.idCarga DESC;");

                return lista.ToList();
            }
        }




        public async Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
        SELECT 
    C.[idCarga],
    C.[idUsuario],
    C.[idRol],
    U.[nombre] AS Responsable,
    M.[motivo],
    M.[idMotivo],
    C.[estado],
    C.[comentario],
    C.[evidencia],
    C.[fecha_autorizacion],
    C.[fecha_creacion],
    C.[llave_ex],
    MD.[nombreusuario],
    MD.[perfil],
    MD.[cargospr],
    G.[Nom_Gerencia]
FROM 
    comentarios C
LEFT JOIN 
    motivo M ON C.idMotivo = M.idMotivo
LEFT JOIN 
    im_matriz_diaria MD ON C.idCarga = MD.idCarga
LEFT JOIN 
    usuario U ON C.[idUsuario] = U.idUsuario
LEFT JOIN 
    im_gerencia G ON U.ID_gerencia = G.ID_gerencia
WHERE 
    U.ID_gerencia = (
        SELECT ID_gerencia FROM usuario WHERE idUsuario = @idUsuarioLogueado
    )
ORDER BY 
    C.idCarga DESC;";

                var lista = await dbdapper.QueryAsync<Comentarios>(query, new { idUsuarioLogueado });

                return lista.ToList();
            }
        }
        public async Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
SELECT 
    C.[idCarga],
    C.[idUsuario],
    C.[idRol],
    U.[nombre] AS Responsable,
    M.[motivo],
    M.[idMotivo],
    C.[estado],
    C.[comentario],
    C.[evidencia],
    C.[fecha_autorizacion],
    C.[fecha_creacion],
    C.[llave_ex],
    MD.[nombreusuario],
    MD.[perfil],
    MD.[cargospr]
FROM comentarios C
LEFT JOIN motivo M ON C.idMotivo = M.idMotivo
LEFT JOIN im_matriz_diaria MD ON C.idCarga = MD.idCarga
LEFT JOIN usuario U ON C.idUsuario = U.idUsuario
LEFT JOIN im_gerencia G ON U.ID_gerencia = G.ID_gerencia
WHERE G.ID_gerencia = (
    SELECT F.codGerencia
    FROM im_firma F
    INNER JOIN usuario U ON F.codUsuarioResponsable = U.idUsuario
    WHERE F.idFirma = @idFirma
    AND U.ID_gerencia = F.codGerencia
)
ORDER BY C.idCarga DESC;";

                var excepciones = await db.QueryAsync<Comentarios>(query, new { idFirma });
                return excepciones.ToList();
            }
        }




    }
}
