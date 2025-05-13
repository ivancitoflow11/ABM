using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Net;
using System.Net.Http;

namespace ABM.Servicios
{
    public interface IRepositorioExcepciones
    {
        //Task GuardarComentariosExcepcion(Comentarios excepcion);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int idpais, int idnegocio);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int idpais, int idnegocio);
        //Task<List<Comentarios>> ObtenerListaHistoricaExcepciones();
        Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion();
        Task<IEnumerable<TipoExcepcion>> ObtenerTiposExcepciones();
        Task<TipoExcepcion> ObtenerTiposExecepcionesPorId(int Id);

        //Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado);
        //Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma);
    }
    public class RepositorioExcepciones : IRepositorioExcepciones
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioExcepciones(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }


        public async Task<IEnumerable<TipoExcepcion>> ObtenerTiposExcepciones()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<TipoExcepcion>(@"SELECT idMotivo,
                     motivo as nombreMotivo
                FROM ftc_motivo");
            }
        }

        public async Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<SistemaExcepcion>(@"SELECT AL3.sistema, 
                AL1.idPaisNegocioSistema, 
                Count (AL1.rutdni) as qty
                FROM ftc_matriz_diaria AL1, 
                dbo.ftc_pais_negocio_sistema AL2,
                dbo.ftc_sistema AL3, dbo.ftc_pnsjt AL4
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


        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                var modelo = await dbdapper.QueryAsync<DetalleExcepcion>(@"
                WITH UniqueRutDni AS (
                SELECT
                    AL1.idPaisNegocioSistema,
                    AL3.sistema, 
                    FP.pais,    
                    FN.negocio,
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
                    ROW_NUMBER() OVER (PARTITION BY CONCAT(AL1.rutdni, AL1.userid) ORDER BY AL1.idCarga DESC) AS rn
                FROM
                    dbo.ftc_matriz_diaria AL1
                INNER JOIN
                    dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                INNER JOIN
                    dbo.ftc_pais FP ON AL2.idPais = FP.idPais 
                INNER JOIN
                    dbo.ftc_negocio FN ON AL2.idNegocio = FN.idNegocio 
                INNER JOIN
                    dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema
                INNER JOIN
                    dbo.ftc_pnsjt AL4 ON AL2.idPaisNegocioSistema = AL4.idPaisNegocioSistema
                                       AND AL1.estado_ex = 'PENDIENTE' 
                LEFT JOIN
                    (SELECT
                        rutdni,
                        nombreusuario,
                        idPaisNegocioSistema,
                        MAX(Nomccostospr) AS Nomccostospr
                     FROM
                        dbo.ftc_agrupa_activos
                     GROUP BY
                        rutdni, nombreusuario, idPaisNegocioSistema) AA
                ON
                    AL1.rutdni = AA.rutdni
                    AND AL1.nombreusuario = AA.nombreusuario
                    AND AL1.idPaisNegocioSistema = AA.idPaisNegocioSistema
                INNER JOIN 
                    dbo.ftc_Subgerencias S ON AA.Nomccostospr = S.Nom_Subgerencia
                INNER JOIN 
                    dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
                LEFT JOIN
                    dbo.ftc_usuario U ON U.ID_gerencia = G.ID_gerencia 
                WHERE
                    AL1.estado_ex <> 'CERRADO'
                    AND FP.idPais = @idpais          
                    AND FN.idNegocio = @idnegocio    
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO')
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND AL1.perfil IS NOT NULL
            )
            SELECT
                UniqueRutDni.* 
            FROM UniqueRutDni
            WHERE rn = 1;", parametros);
                return modelo;
            }
        }

        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };
                var modelo = await dbdapper.QueryAsync<DetalleExcepcion>(@"WITH UniqueRutDni AS (
                SELECT
                    AL1.idPaisNegocioSistema,
                    AL3.sistema,        
                    FP.pais,          
                    FN.negocio,         
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
                    ROW_NUMBER() OVER (PARTITION BY CONCAT(AL1.rutdni, AL1.userid) ORDER BY AL1.idCarga DESC) AS rn 
                FROM
                    dbo.ftc_matriz_diaria AL1
                INNER JOIN
                    dbo.ftc_pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                INNER JOIN
                    dbo.ftc_pais FP ON AL2.idPais = FP.idPais 
                INNER JOIN
                    dbo.ftc_negocio FN ON AL2.idNegocio = FN.idNegocio 
                INNER JOIN
                    dbo.ftc_sistema AL3 ON AL3.idSistema = AL2.idSistema 
                INNER JOIN
                    dbo.ftc_pnsjt AL4 ON AL2.idPaisNegocioSistema = AL4.idPaisNegocioSistema
                                       AND AL1.estado_ex = 'PENDIENTE' 
                LEFT JOIN
                    (SELECT
                        rutdni,
                        nombreusuario,
                        idPaisNegocioSistema,
                        MAX(Nomccostospr) AS Nomccostospr 
                     FROM
                        dbo.ftc_agrupa_activos
                     GROUP BY
                        rutdni, nombreusuario, idPaisNegocioSistema) AA
                ON
                    AL1.rutdni = AA.rutdni
                    AND AL1.nombreusuario = AA.nombreusuario
                    AND AL1.idPaisNegocioSistema = AA.idPaisNegocioSistema
                LEFT JOIN
                    dbo.ftc_Subgerencias S ON AA.Nomccostospr = S.Nom_Subgerencia 
                LEFT JOIN
                    dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia 
                WHERE
                    AL1.estado_ex <> 'CERRADO'
                    AND FP.idPais = @idpais         
                    AND FN.idNegocio = @idnegocio   
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO')
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND AL1.perfil IS NOT NULL
            )
            SELECT
                UniqueRutDni.* 
            FROM UniqueRutDni
            WHERE rn = 1;", parametros);

                return modelo;
            }
        }

        public async Task<TipoExcepcion> ObtenerTiposExecepcionesPorId(int Id)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<TipoExcepcion>(@"SELECT idMotivo,
                     motivo as nombreMotivo
                FROM ftc_motivo WHERE idMotivo = @Id", new { Id });
            }
        }

        //HASTA AQUI OK




//        public async Task GuardarComentariosExcepcion(Comentarios excepcion)
//        {
//            using (IDbConnection dbdapper = new SqlConnection(connectionString))
//            {
//                await dbdapper.ExecuteAsync(@"INSERT INTO comentarios
//                           (idCarga
//                           ,idUsuario
//                           ,idRol
//                           ,idMotivo
//                           ,estado
//                           ,comentario
//                           ,evidencia
//                           ,fecha_autorizacion
//                           ,fecha_creacion
//                           ,llave_ex)
//                     VALUES
//                           (@idCarga
//                           ,@idUsuario
//                           ,@idRol
//                           ,@idMotivo
//                           ,@estado
//                           ,@comentario
//                           ,@evidencia
//                           ,@fecha_autorizacion
//                           ,@fecha_creacion
//                           ,@llave_ex)", excepcion);
//            }
//        }


//        public async Task<List<Comentarios>> ObtenerListaHistoricaExcepciones()
//        {
//            using (IDbConnection dbdapper = new SqlConnection(connectionString))
//            {
//                var lista = await dbdapper.QueryAsync<Comentarios>(@"SELECT 
//    C.[idCarga],
//    C.[idUsuario],
//    C.[idRol],
//    U.[nombre] AS Responsable,
//    M.[motivo],
//    M.[idMotivo],
//    C.[estado],
//    C.[comentario],
//    C.[evidencia],
//    C.[fecha_autorizacion],
//    C.[fecha_creacion],
//    C.[llave_ex],
//    MD.[nombreusuario],
//    MD.[perfil],
//    MD.[cargospr],
//    G.[Nom_Gerencia]
//FROM 
//    comentarios C
//LEFT JOIN 
//    motivo M ON C.idMotivo = M.idMotivo
//LEFT JOIN 
//    im_matriz_diaria MD ON C.idCarga = MD.idCarga
//LEFT JOIN 
//    usuario U ON C.[idUsuario] = U.idUsuario
//LEFT JOIN 
//    im_gerencia G ON U.ID_gerencia = G.ID_gerencia
//ORDER BY 
//    C.idCarga DESC;");

//                return lista.ToList();
//            }
//        }




//        public async Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado)
//        {
//            using (IDbConnection dbdapper = new SqlConnection(connectionString))
//            {
//                var query = @"
//        SELECT 
//    C.[idCarga],
//    C.[idUsuario],
//    C.[idRol],
//    U.[nombre] AS Responsable,
//    M.[motivo],
//    M.[idMotivo],
//    C.[estado],
//    C.[comentario],
//    C.[evidencia],
//    C.[fecha_autorizacion],
//    C.[fecha_creacion],
//    C.[llave_ex],
//    MD.[nombreusuario],
//    MD.[perfil],
//    MD.[cargospr],
//    G.[Nom_Gerencia]
//FROM 
//    comentarios C
//LEFT JOIN 
//    motivo M ON C.idMotivo = M.idMotivo
//LEFT JOIN 
//    im_matriz_diaria MD ON C.idCarga = MD.idCarga
//LEFT JOIN 
//    usuario U ON C.[idUsuario] = U.idUsuario
//LEFT JOIN 
//    im_gerencia G ON U.ID_gerencia = G.ID_gerencia
//WHERE 
//    U.ID_gerencia = (
//        SELECT ID_gerencia FROM usuario WHERE idUsuario = @idUsuarioLogueado
//    )
//ORDER BY 
//    C.idCarga DESC;";

//                var lista = await dbdapper.QueryAsync<Comentarios>(query, new { idUsuarioLogueado });

//                return lista.ToList();
//            }
//        }
//        public async Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma)
//        {
//            using (IDbConnection db = new SqlConnection(connectionString))
//            {
//                var query = @"
//SELECT 
//    C.[idCarga],
//    C.[idUsuario],
//    C.[idRol],
//    U.[nombre] AS Responsable,
//    M.[motivo],
//    M.[idMotivo],
//    C.[estado],
//    C.[comentario],
//    C.[evidencia],
//    C.[fecha_autorizacion],
//    C.[fecha_creacion],
//    C.[llave_ex],
//    MD.[nombreusuario],
//    MD.[perfil],
//    MD.[cargospr]
//FROM comentarios C
//LEFT JOIN motivo M ON C.idMotivo = M.idMotivo
//LEFT JOIN im_matriz_diaria MD ON C.idCarga = MD.idCarga
//LEFT JOIN usuario U ON C.idUsuario = U.idUsuario
//LEFT JOIN im_gerencia G ON U.ID_gerencia = G.ID_gerencia
//WHERE G.ID_gerencia = (
//    SELECT F.codGerencia
//    FROM im_firma F
//    INNER JOIN usuario U ON F.codUsuarioResponsable = U.idUsuario
//    WHERE F.idFirma = @idFirma
//    AND U.ID_gerencia = F.codGerencia
//)
//ORDER BY C.idCarga DESC;";

//                var excepciones = await db.QueryAsync<Comentarios>(query, new { idFirma });
//                return excepciones.ToList();
//            }
//        }

    }
}
