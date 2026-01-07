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
        Task GuardarComentariosExcepcion(Comentarios excepcion);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int idpais, int idnegocio);
        Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int idpais, int idnegocio);
        Task<List<Comentarios>> ObtenerListaHistoricaExcepciones();
        Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion(int idpais, int idnegocio);
        Task<IEnumerable<TipoExcepcion>> ObtenerTiposExcepciones();
        Task<TipoExcepcion> ObtenerTiposExecepcionesPorId(int Id);
        Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado);
        Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma);
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

        public async Task<IEnumerable<SistemaExcepcion>> ObtenerListaSistemaConExcepcion(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                // CAMBIO: Simplificado usando ftc_pns_2. 
                // Ya trae los nombres y filtra solo activos.
                var modelo = await dbdapper.QueryAsync<SistemaExcepcion>(@"
            SELECT
                AL2.sistema,           
                AL1.idPaisNegocioSistema,    
                AL2.pais AS pais,            
                AL2.negocio AS negocio,      
                COUNT(AL1.rutdni) AS qty    
            FROM
                ftc_matriz_diaria AL1
            -- CRUCE OBLIGATORIO CON PNS_2 (Sistemas Activos)
            INNER JOIN
                dbo.ftc_pns_2 AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            INNER JOIN
                dbo.ftc_pnsjt AL4 ON AL2.idPaisNegocioSistema = AL4.idPaisNegocioSistema
            WHERE
                AL1.estado_ex <> 'CERRADO'
                AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO')
                AND AL1.fechaAutorizacion_ex IS NULL
                AND AL2.idPais = @idpais          
                AND AL2.idNegocio = @idnegocio   
            GROUP BY
                AL2.sistema,
                AL1.idPaisNegocioSistema,
                AL2.pais,      
                AL2.negocio      
            ORDER BY
                2;", parametros);
                return modelo;
            }
        }

        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorGerencia(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                // CAMBIO: Reemplazo de ftc_pais_negocio_sistema/ftc_pais/ftc_negocio por ftc_pns_2
                var modelo = await dbdapper.QueryAsync<DetalleExcepcion>(@"
                WITH UniqueRutDni AS (
                SELECT
                    AL1.idPaisNegocioSistema,
                    AL2.sistema, 
                    AL2.pais,     
                    AL2.negocio,
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
                -- CRUCE CON PNS_2
                INNER JOIN
                    dbo.ftc_pns_2 AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
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
                    AND AL2.idPais = @idpais           
                    AND AL2.idNegocio = @idnegocio     
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO')
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND AL1.perfil IS NOT NULL
            )
            SELECT
                UniqueRutDni.* FROM UniqueRutDni
            WHERE rn = 1;", parametros);
                return modelo;
            }
        }

        public async Task<IEnumerable<DetalleExcepcion>> ObtenerListaDetalleExcepcionPorIdSistema(int idpais, int idnegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new { idpais, idnegocio };

                // CAMBIO: Reemplazo de tablas individuales por ftc_pns_2
                var modelo = await dbdapper.QueryAsync<DetalleExcepcion>(@"WITH UniqueRutDni AS (
                SELECT
                    AL1.idPaisNegocioSistema,
                    AL2.sistema,        
                    AL2.pais,           
                    AL2.negocio,          
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
                -- CRUCE CON PNS_2
                INNER JOIN
                    dbo.ftc_pns_2 AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
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
                    AND AL2.idPais = @idpais          
                    AND AL2.idNegocio = @idnegocio    
                    AND AL1.estadousuario IN ('ACTIVO', 'EXTERNO')
                    AND AL1.fechaAutorizacion_ex IS NULL
                    AND AL1.perfil IS NOT NULL
            )
            SELECT
                UniqueRutDni.* FROM UniqueRutDni
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

        public async Task GuardarComentariosExcepcion(Comentarios excepcion)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                await dbdapper.ExecuteAsync(@"INSERT INTO ftc_comentarios
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
                // CAMBIO: Agregado INNER JOIN con ftc_pns_2 para filtrar historial de sistemas activos
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
            ftc_comentarios C
        LEFT JOIN 
            ftc_motivo M ON C.idMotivo = M.idMotivo
        LEFT JOIN 
            ftc_matriz_diaria MD ON C.idCarga = MD.idCarga
        -- FILTRO DE ACTIVOS APLICADO AL HISTORIAL
        INNER JOIN
            dbo.ftc_pns_2 PNS ON MD.idPaisNegocioSistema = PNS.idPaisNegocioSistema
        LEFT JOIN 
            ftc_usuario U ON C.[idUsuario] = U.idUsuario
        LEFT JOIN 
            ftc_gerencia G ON U.ID_gerencia = G.ID_gerencia
        ORDER BY 
            C.idCarga DESC;");

                return lista.ToList();
            }
        }

        public async Task<List<Comentarios>> ObtenerListaHistoricaExcepcionesPorUsuario(int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // CAMBIO: Reemplazo de joins antiguos por ftc_pns_2
                var query = @"
                            SELECT 
                            C.[idCarga], C.[idUsuario], C.[idRol], U.[nombre] AS Responsable, M.[motivo], M.[idMotivo],
                            C.[estado], C.[comentario], C.[evidencia], C.[fecha_autorizacion], C.[fecha_creacion],
                            C.[llave_ex], 
                            MD.[nombreusuario], MD.[perfil], MD.[cargospr], 
                            G.[Nom_Gerencia],
                            PNS.pais AS Pais,       
                            PNS.negocio AS Negocio   
                        FROM ftc_comentarios C
                        LEFT JOIN ftc_motivo M ON C.idMotivo = M.idMotivo
                        LEFT JOIN ftc_matriz_diaria MD ON C.idCarga = MD.idCarga 
                        LEFT JOIN ftc_usuario U ON C.[idUsuario] = U.idUsuario
                        LEFT JOIN ftc_gerencia G ON U.ID_gerencia = G.ID_gerencia
                        -- CRUCE CON PNS_2
                        INNER JOIN dbo.ftc_pns_2 PNS ON MD.idPaisNegocioSistema = PNS.idPaisNegocioSistema
                        WHERE U.idUsuario = @idUsuarioLogueado
                        ORDER BY C.idCarga DESC;";

                var lista = await dbdapper.QueryAsync<Comentarios>(query, new { idUsuarioLogueado });

                return lista.ToList();
            }
        }

        public async Task<List<Comentarios>> ObtenerExcepcionesPorFirma(int idFirma)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // CAMBIO: Agregado filtro de PNS_2 para asegurar consistencia
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
            FROM ftc_comentarios C
            LEFT JOIN ftc_motivo M ON C.idMotivo = M.idMotivo
            LEFT JOIN ftc_matriz_diaria MD ON C.idCarga = MD.idCarga
            -- FILTRO DE ACTIVOS
            INNER JOIN dbo.ftc_pns_2 PNS ON MD.idPaisNegocioSistema = PNS.idPaisNegocioSistema
            LEFT JOIN ftc_usuario U ON C.idUsuario = U.idUsuario
            LEFT JOIN ftc_gerencia G ON U.ID_gerencia = G.ID_gerencia
            WHERE G.ID_gerencia = (
                SELECT F.codGerencia
                FROM ftc_firma F
                INNER JOIN ftc_usuario U ON F.codUsuarioResponsable = U.idUsuario
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