using Dapper;
using ABM.Data;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace ABM.Servicios
{
	public interface IRepositorioCargas
	{
        Task<IEnumerable<Abm_Sistema>> ObtenerListaSistemasMatriz();
        Task<Abm_Sistema> ObtenerSistemasMatrizPorId(int Idsistema);
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizPorGerencia(int idUsuarioLogueado);
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatriz(int IdSistema);
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizTodasGerencias();
        Task<bool> InsertarFirmaYDetalles(ImFirma firma, List<ImDetalleFirma> detallesFirma);
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerResumenMatrizPorGerencia(int idUsuarioLogueado);

        Task<DateTime?> ObtenerUltimaFechaDeCarga();
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerResumenMatrizTodasGerencias();
        Task<IEnumerable<cl_sodimac_matriz_perfil>> FirmasMatrizPorGerenciaYFirma(int idUsuarioLogueado, int idFirma);

        Task<IEnumerable<cl_sodimac_matriz_perfil>> FirmasMatrizTodasGerenciasPorFirma(int idFirma);
        Task<DateTime?> ObtenerUltimaFirmaDelMes(int codUsuarioResponsable, int? codGerencia);
        Task<(ImFirma firma, List<ImDetalleFirma> detalles)> ObtenerUltimaFirmaYDetalles(int codUsuarioResponsable, int? codGerencia, string fechaCarga = null);
		Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizRespaldo(int IdSistema);
        Task<IEnumerable<Abm_Sistema>> ObtenerListaSistemaManual();
        Task<Abm_Sistema> ObtenerSistemaManualPorId(int Idsistema);
        Task RegistrarDatosMatrizPerfil(IEnumerable<cl_sodimac_matriz_perfil> dataAnterior,
            int IdSistema, List<cl_sodimac_matriz_perfil> dataNueva);
        Task<IEnumerable<Abm_Sistema>> ListaDeSistemas();
        Task<IEnumerable<Abm_Sistema>> SistemaCargoPerfil();
        Task<IEnumerable<Abm_Sistema>> SistemasMatriz();
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaSinPerfilTodasGerencias();
        Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaSinPerfilPorGerencia(int idUsuarioLogueado);

        Task<DateTime?> ObtenerUltimaFirma(int idUsuario);


    }
	public class RepositorioCargas : IRepositorioCargas
	{
		private readonly string connectionString;

		public RepositorioCargas(IConfiguration configuration)
		{
			connectionString = configuration.GetConnectionString("CadenaSQL") + ";Command Timeout=120"; 
		}

		public async Task<IEnumerable<Abm_Sistema>> ObtenerListaSistemasMatriz()
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<Abm_Sistema>(@"SELECT AL3.sistema, 
                AL2.idPaisNegocioSistema as idSistema
                FROM dbo.pais AL1, 
                dbo.pais_negocio_sistema AL2,
                dbo.sistema AL3, 
                dbo.pnsjt AL4 WHERE 
                (AL2.idPais=AL1.idPais 
                AND AL3.idSistema=AL2.idSistema 
                AND AL4.idPaisNegocioSistema=AL2.idPaisNegocioSistema)
                AND ((AL1.pais='IMPERIAL' AND AL4.infomatrizperfil='NO'))");
			}
		}

		public async Task<IEnumerable<Abm_Sistema>> ListaDeSistemas()
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<Abm_Sistema>(@"SELECT AL3.sistema, 
                idSistema
                FROM dbo.sistema AL3");
			}
		}

        public async Task<DateTime?> ObtenerUltimaFirma(int idUsuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                const string query = @"
            SELECT TOP 1 fechaFirma
            FROM [ABM_SOFTWARE].[dbo].[im_firma]
            WHERE codUsuarioResponsable = @idUsuario
            ORDER BY fechaFirma DESC;
        ";
                var result = await dbdapper.QueryFirstOrDefaultAsync<DateTime?>(query, new { idUsuario });
                return result;
            }
        }




        public async Task<IEnumerable<Abm_Sistema>> SistemaCargoPerfil()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Abm_Sistema>(@"SELECT AL3.sistema, 
                idSistema
                FROM dbo.sistema AL3
				where idSistema = 1");
            }
        }

        public async Task<IEnumerable<Abm_Sistema>> SistemasMatriz()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Abm_Sistema>(@"SELECT DISTINCT AL3.sistema, 
                    AL2.idPaisNegocioSistema as idSistema
                    FROM dbo.pais AL1, 
                    dbo.pais_negocio_sistema AL2,
                    dbo.sistema AL3, 
                    dbo.pnsjt AL4 WHERE 
                    (AL2.idPais=AL1.idPais 
                    AND AL3.idSistema=AL2.idSistema 
                    AND AL4.idPaisNegocioSistema=AL2.idPaisNegocioSistema)
                    AND ((AL1.pais='IMPERIAL' AND AL4.info_gestion='NO')) 
                    AND AL4.idPaisNegocioSistema  = 1");
            }
        }


        public async Task<Abm_Sistema> ObtenerSistemasMatrizPorId(int Idsistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT AL3.sistema, 
                   AL2.idPaisNegocioSistema as idSistema
            FROM dbo.pais AL1
            JOIN dbo.pais_negocio_sistema AL2 ON AL2.idPais = AL1.idPais
            JOIN dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN dbo.pnsjt AL4 ON AL4.idPaisNegocioSistema = AL2.idPaisNegocioSistema
            WHERE AL1.pais = 'IMPERIAL'
              AND AL4.info_gestion = 'NO'
              AND AL4.idPaisNegocioSistema = @Idsistema";

                var sistema = await dbdapper.QueryFirstOrDefaultAsync<Abm_Sistema>(query, new { Idsistema });

                if (sistema == null)
                {
                    throw new Exception("No se encontró un sistema con el Id proporcionado.");
                }

                return sistema;
            }
        }

        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatriz(int IdSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"SELECT AL1.modulo, 
                    AL1.perfil, 
                    AL1.codPerfil,
                    AL1.Fcarga, 
                    AL1.responsable, 
                    AL1.cargoActivo, 
                    AL1.cargo, 
                    AL1.idNivelCargo, 
                    AL1.idPaisNegocioSistema, 
                    AL1.idUsuario, 
                    AL1.estado_matriz, 
                    AL1.matriz, 
                    AL1.usocargo, 
                    AL1.tipocarga, 
                    AL1.nivelcargo 
                    FROM dbo.im_sodimac_matriz_perfil AL1 
                    WHERE (AL1.idPaisNegocioSistema=@IdSistema)", new { IdSistema });

            }
        }


		public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerResumenMatrizPorGerencia(int idUsuarioLogueado)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"
            SELECT DISTINCT 
                AL4.pais, 
                AL3.sistema, 
                G.Nom_Gerencia AS Gerencia,
                S.Nom_Subgerencia AS Subgerencia,
                AL1.cargospr, 
                AL1.perfil, 
                COUNT(DISTINCT AL1.rutdni) AS CantidadUsuarios
            FROM 
                dbo.im_agrupa_activos AL1
            JOIN 
                dbo.pais_negocio_sistema AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
            JOIN 
                dbo.sistema AL3 ON AL3.idSistema = AL2.idSistema
            JOIN 
                dbo.pais AL4 ON AL4.idPais = AL2.idPais
            JOIN 
                dbo.im_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
            JOIN 
                dbo.im_gerencia G ON S.COD_Gerencia = G.ID_gerencia
            LEFT JOIN 
                dbo.usuario U ON G.ID_gerencia = U.ID_gerencia
            WHERE 
                AL3.sistema = 'ADAPI' 
                AND AL4.pais = 'IMPERIAL'
                AND AL1.Nomccostospr IS NOT NULL
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
                    -- Si el usuario tiene una subgerencia asignada, filtramos por esa subgerencia
                    S.Nom_Subgerencia = (
                        SELECT Nom_Subgerencia
                        FROM dbo.im_Subgerencias
                        WHERE ID_Subgerencia = (
                            SELECT ID_Subgerencia
                            FROM dbo.usuario
                            WHERE IdUsuario = @idUsuarioLogueado
                        )
                    )
                    -- Si el usuario NO tiene subgerencia, mostramos todas las subgerencias
                    OR (
                        SELECT ID_Subgerencia
                        FROM dbo.usuario 
                        WHERE IdUsuario = @idUsuarioLogueado
                    ) IS NULL
                )
            GROUP BY 
                AL4.pais, 
                AL3.sistema, 
                G.Nom_Gerencia,
                S.Nom_Subgerencia,
                AL1.cargospr, 
                AL1.perfil;
                ", new { idUsuarioLogueado });
			}
		}


		public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerResumenMatrizTodasGerencias()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"                   
                SELECT DISTINCT 
                AL4.pais, 
                AL3.sistema, 
                G.Nom_Gerencia AS Gerencia,
                S.Nom_Subgerencia AS Subgerencia,
                AL1.cargospr, 
                AL1.perfil, 
                COUNT(DISTINCT AL1.rutdni) AS CantidadUsuarios
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
                AL3.sistema = 'ADAPI' 
                AND AL4.pais = 'IMPERIAL' 
                AND AL1.Nomccostospr IS NOT NULL
            GROUP BY 
                AL4.pais, 
                AL3.sistema, 
                G.Nom_Gerencia,
                S.Nom_Subgerencia,
                AL1.cargospr, 
                AL1.perfil;
                ");
            }
        }


        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizPorGerencia(int idUsuarioLogueado)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"
                WITH UsuarioActual AS (
    SELECT 
        U.ID_gerencia, 
        U.ID_Subgerencia AS UsuarioSubgerenciaId,
        S.Nom_Subgerencia
    FROM dbo.usuario U
    LEFT JOIN dbo.im_Subgerencias S ON U.ID_Subgerencia = S.ID_Subgerencia
    WHERE U.IdUsuario = @idUsuarioLogueado
),
UniqueRutDni AS (
    SELECT 
        AL4.pais, 
        AL3.sistema, 
        AL1.idPaisNegocioSistema, 
        AL1.rutdni, 
        AL1.dv, 
        AL1.nombreusuario, 
        AL1.userid, 
        AL1.cargospr, 
        AL1.perfil, 
        AL1.codccosto,
        AL1.codccostospr, 
        AL1.Nomccosto, 
        AL1.cargomatriz, 
        AL1.perfilmatriz, 
        AL1.feccarga, 
        AL1.cta_duplicada,
        CASE 
            WHEN EXISTS (SELECT 1 FROM comentarios WHERE llave_ex = AL1.llave_ex) THEN 'Autorizado'
            ELSE 'Correcto' 
        END AS [EstadoMatrizPerfil],
        G.ID_gerencia, 
        G.Nom_Gerencia, 
        S.ID_Subgerencia AS SubgerenciaId,
        S.Nom_Subgerencia,
        U.IdUsuario,
        ISNULL((SELECT TOP 1 evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') AS Evidencia,
        -- Obtener la fecha de la última firma del mes actual
                    (SELECT MAX(f.fechaFirma) 
                     FROM im_firma f 
                     WHERE f.codGerencia = G.ID_gerencia 
                     AND YEAR(f.fechaFirma) = YEAR(GETDATE()) 
                     AND MONTH(f.fechaFirma) = MONTH(GETDATE())
                    ) AS UltimaFirmaMesActual,
        ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.feccarga DESC) AS rn
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
    LEFT JOIN 
        dbo.usuario U ON G.ID_gerencia = U.ID_gerencia AND U.IdUsuario = @idUsuarioLogueado 
    LEFT JOIN UsuarioActual UA ON G.ID_gerencia = UA.ID_gerencia
    WHERE 
        AL3.sistema = 'ADAPI' 
        AND AL4.pais = 'IMPERIAL'
        AND (
            G.ID_gerencia = UA.ID_gerencia
            OR S.ID_Subgerencia = UA.UsuarioSubgerenciaId
        )
        AND (
            S.Nom_Subgerencia = UA.Nom_Subgerencia 
            OR UA.UsuarioSubgerenciaId IS NULL
        )
        AND AL1.perfil IS NOT NULL
)
SELECT *
FROM UniqueRutDni
WHERE rn = 1;
                ", new { idUsuarioLogueado });
            }
        }



        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizTodasGerencias()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"
            WITH UniqueRutDni AS (
    SELECT 
    AL4.pais, 
    AL3.sistema, 
    AL1.idPaisNegocioSistema, 
    AL1.rutdni, 
    AL1.dv, 
    AL1.nombreusuario, 
    AL1.userid, 
    AL1.cargospr, 
    AL1.perfil, 
    AL1.codccosto, 
    AL1.codccostospr, 
    AL1.Nomccosto, 
    AL1.cargomatriz, 
    AL1.perfilmatriz, 
    AL1.feccarga, 
    AL1.cta_duplicada,
    CASE 
            WHEN ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') != 'Sin evidencia' THEN 'Autorizado'
            ELSE 'Correcto' 
        END AS [EstadoMatrizPerfil],
    G.ID_gerencia, 
    G.Nom_Gerencia, 
    S.ID_Subgerencia,
    S.Nom_Subgerencia,
    U.IdUsuario,
    ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') AS Evidencia,
    -- Obtener la fecha de la última firma del mes actual
                    (SELECT MAX(f.fechaFirma) 
                     FROM im_firma f 
                     WHERE f.codGerencia = G.ID_gerencia 
                     AND YEAR(f.fechaFirma) = YEAR(GETDATE()) 
                     AND MONTH(f.fechaFirma) = MONTH(GETDATE())
                    ) AS UltimaFirmaMesActual,
    ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.feccarga DESC) AS rn  -- Numeramos las filas por cada rutdni
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
LEFT JOIN 
    dbo.usuario U ON G.ID_gerencia = U.ID_gerencia
WHERE 
    AL3.sistema = 'ADAPI' 
    AND AL4.pais = 'IMPERIAL'
    AND AL1.perfil IS NOT NULL
)
SELECT *
FROM UniqueRutDni
WHERE rn = 1;");
            }
        }


        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaSinPerfilPorGerencia(int idUsuarioLogueado)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"                    
                WITH UniqueRutDni AS (
                    SELECT 
                        AL4.pais, 
                        AL3.sistema, 
                        AL1.idPaisNegocioSistema, 
                        AL1.rutdni, 
                        AL1.dv, 
                        AL1.nombreusuario, 
                        AL1.userid, 
                        AL1.cargospr, 
                        AL1.perfil, 
                        AL1.codccosto,
                        AL1.codccostospr, 
                        AL1.Nomccosto, 
                        AL1.cargomatriz, 
                        AL1.perfilmatriz, 
                        AL1.feccarga, 
                        AL1.cta_duplicada,
                        CASE 
                            WHEN AL1.perfil IS NULL AND ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') != 'Sin evidencia' THEN 'Autorizado'
                            WHEN AL1.perfil IS NULL THEN 'Incorrecto' 
                            ELSE 'Correcto' 
                        END AS [EstadoMatrizPerfil],
                        G.ID_gerencia, 
                        G.Nom_Gerencia, 
                        S.ID_Subgerencia,
                        S.Nom_Subgerencia,
                        U.IdUsuario,
                        ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') AS Evidencia,
                        CASE 
                            WHEN EXISTS (
                                SELECT 1 
                                FROM im_firma f 
                                INNER JOIN im_detalle_firma df ON f.IdFirma = df.codFirma
                                WHERE f.codUsuarioResponsable = @idUsuarioLogueado 
                                AND f.codGerencia = G.ID_gerencia 
                                AND df.feccarga = AL1.feccarga
                            ) THEN 1
                            ELSE 0
                        END AS Firmado,
                        ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.feccarga DESC) AS rn  -- Numeramos las filas por cada `rutdni`
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
                    LEFT JOIN 
                        dbo.usuario U ON G.ID_gerencia = U.ID_gerencia
                    WHERE 
                        AL3.sistema = 'ADAPI' 
                        AND AL4.pais = 'IMPERIAL'
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
                        -- Aquí se añade la condición que filtra solo los registros correspondientes a la subgerencia del usuario
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
                WHERE rn = 1
                AND [EstadoMatrizPerfil] = 'Incorrecto';
                ", new { idUsuarioLogueado });
			}
		}


		public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaSinPerfilTodasGerencias()
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"
                    WITH UniqueRutDni AS (
                    SELECT 
                        AL4.pais, 
                        AL3.sistema, 
                        AL1.idPaisNegocioSistema, 
                        AL1.rutdni, 
                        AL1.dv, 
                        AL1.nombreusuario, 
                        AL1.userid, 
                        AL1.cargospr, 
                        AL1.perfil, 
                        AL1.codccosto, 
                        AL1.codccostospr, 
                        AL1.Nomccosto, 
                        AL1.cargomatriz, 
                        AL1.perfilmatriz, 
                        AL1.feccarga, 
                        AL1.cta_duplicada,
                        CASE 
                            WHEN AL1.perfil IS NULL AND ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') != 'Sin evidencia' THEN 'Autorizado'
                            WHEN AL1.perfil IS NULL THEN 'Incorrecto' 
                            ELSE 'Correcto' 
                        END AS [EstadoMatrizPerfil],
                        G.ID_gerencia, 
                        G.Nom_Gerencia, 
                        S.ID_Subgerencia,
                        S.Nom_Subgerencia,
                        U.IdUsuario,
                        ISNULL((SELECT evidencia FROM comentarios WHERE llave_ex = AL1.llave_ex), 'Sin evidencia') AS Evidencia,
                        CASE 
                            WHEN EXISTS (
                                SELECT 1 
                                FROM im_firma f 
                                INNER JOIN im_detalle_firma df ON f.IdFirma = df.codFirma
                                WHERE f.codUsuarioResponsable = U.IdUsuario 
                                AND f.codGerencia = G.ID_gerencia 
                                AND df.feccarga = AL1.feccarga
                            ) THEN 1
                            ELSE 0
                        END AS Firmado,
                        ROW_NUMBER() OVER (PARTITION BY AL1.rutdni ORDER BY AL1.feccarga DESC) AS rn  -- Numeramos las filas por cada `rutdni`
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
                    LEFT JOIN 
                        dbo.usuario U ON G.ID_gerencia = U.ID_gerencia
                    WHERE 
                        AL3.sistema = 'ADAPI' 
                        AND AL4.pais = 'IMPERIAL'
                )
                SELECT *
                FROM UniqueRutDni
                WHERE rn = 1
                AND [EstadoMatrizPerfil] = 'Incorrecto';");
			}
		}

        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> FirmasMatrizPorGerenciaYFirma(int idUsuarioLogueado, int idFirma)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
WITH UniqueRutDni AS (
    SELECT 
        df.pais, 
        df.sistema, 
        df.idPaisNegocioSistema, 
        df.rutdni, 
        df.dv, 
        df.nombreusuario, 
        df.userid, 
        df.cargospr, 
        df.perfil, 
        df.codccosto, 
        df.codccostospr, 
        df.Nomccosto, 
        df.cargomatriz, 
        df.perfilmatriz, 
        df.feccarga, 
        df.cta_duplicada,
        f.codGerencia, 
        g.Nom_Gerencia,
        f.comentario,
        f.fechaFirma,
        u.nombre AS nombreResponsable,
        u.firma AS firmaResponsable,
        s.ID_Subgerencia,
        s.Nom_Subgerencia,
        ROW_NUMBER() OVER (PARTITION BY df.rutdni ORDER BY f.fechaFirma DESC) AS rn,
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM im_firma f2 
                INNER JOIN im_detalle_firma df2 ON f2.IdFirma = df2.codFirma
                WHERE f2.codGerencia = f.codGerencia 
                AND f2.codUsuarioResponsable = (
                    SELECT TOP 1 codUsuarioResponsable 
                    FROM im_firma 
                    WHERE codGerencia = f.codGerencia 
                    ORDER BY fechaFirma DESC
                )
            ) THEN 1
            ELSE 0
        END AS Firmado
    FROM 
        im_detalle_firma df
    INNER JOIN 
        im_firma f ON df.codFirma = f.IdFirma
    INNER JOIN 
        im_gerencia g ON f.codGerencia = g.ID_gerencia
    LEFT JOIN 
        im_Subgerencias s ON f.codGerencia = s.COD_Gerencia
    INNER JOIN 
        usuario u ON f.codUsuarioResponsable = u.idUsuario
    WHERE 
        f.IdFirma = @idFirma
        AND f.codGerencia = (
            SELECT ID_gerencia 
            FROM usuario 
            WHERE IdUsuario = @idUsuarioLogueado
        )
)
SELECT *
FROM UniqueRutDni
WHERE rn = 1;";

                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(query, new { idUsuarioLogueado, idFirma });
            }
        }

        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> FirmasMatrizTodasGerenciasPorFirma(int idFirma)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
WITH UniqueRutDni AS (
    SELECT 
        df.pais, 
        df.sistema, 
        df.idPaisNegocioSistema, 
        df.rutdni, 
        df.dv, 
        df.nombreusuario, 
        df.userid, 
        df.cargospr, 
        df.perfil, 
        df.codccosto, 
        df.codccostospr, 
        df.Nomccosto, 
        df.cargomatriz, 
        df.perfilmatriz, 
        df.feccarga, 
        df.cta_duplicada,
        f.codGerencia, 
        g.Nom_Gerencia,
        f.comentario,
        f.fechaFirma,
        u.nombre AS nombreResponsable,
        s.ID_Subgerencia,
        s.Nom_Subgerencia,
        ROW_NUMBER() OVER (PARTITION BY df.rutdni ORDER BY f.fechaFirma DESC) AS rn,
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM im_firma f2 
                INNER JOIN im_detalle_firma df2 ON f2.IdFirma = df2.codFirma
                WHERE f2.codGerencia = f.codGerencia 
                AND df2.feccarga = df.feccarga
            ) THEN 1
            ELSE 0
        END AS Firmado
    FROM 
        im_detalle_firma df
    INNER JOIN 
        im_firma f ON df.codFirma = f.IdFirma
    INNER JOIN 
        im_gerencia g ON f.codGerencia = g.ID_gerencia
    LEFT JOIN 
        im_Subgerencias s ON f.codGerencia = s.COD_Gerencia
    INNER JOIN 
        usuario u ON f.codUsuarioResponsable = u.idUsuario
    WHERE 
        f.IdFirma = @idFirma
)
SELECT *
FROM UniqueRutDni
WHERE rn = 1;";

                return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(query, new { idFirma });
            }
        }



        public async Task<(ImFirma firma, List<ImDetalleFirma> detalles)> ObtenerUltimaFirmaYDetalles(int codUsuarioResponsable, int? codGerencia, string fechaCarga = null)
		{
			using (var dbConnection = new SqlConnection(connectionString))
			{
				await dbConnection.OpenAsync();

				var queryParameters = new DynamicParameters();
				queryParameters.Add("@codUsuarioResponsable", codUsuarioResponsable);
				queryParameters.Add("@codGerencia", codGerencia);

				string whereClause = "WHERE codUsuarioResponsable = @codUsuarioResponsable AND codGerencia = @codGerencia";

				if (!string.IsNullOrEmpty(fechaCarga))
				{
					whereClause += " AND fechaCarga = @fechaCarga";
					queryParameters.Add("@fechaCarga", fechaCarga);
				}

				var queryFirma = $@"
            SELECT TOP 1 *
            FROM im_firma
            {whereClause}
            ORDER BY fechaFirma DESC";

				var firma = await dbConnection.QueryFirstOrDefaultAsync<ImFirma>(queryFirma, queryParameters);

				List<ImDetalleFirma> detalles = new List<ImDetalleFirma>();
				if (firma != null)
				{
					var queryDetalles = @"
                SELECT *
                FROM im_detalle_firma
                WHERE codFirma = @codFirma";

					detalles = (await dbConnection.QueryAsync<ImDetalleFirma>(queryDetalles,
						new { codFirma = firma.IdFirma })).ToList();
				}

				return (firma, detalles);
			}
		}

        public async Task<DateTime?> ObtenerUltimaFirmaDelMes(int codUsuarioResponsable, int? codGerencia)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                var query = @"
        SELECT MAX(f.fechaFirma)
        FROM im_firma f
        WHERE f.codUsuarioResponsable = @codUsuarioResponsable
        AND f.codGerencia = @codGerencia
        AND YEAR(f.fechaFirma) = YEAR(GETDATE())
        AND MONTH(f.fechaFirma) = MONTH(GETDATE())";

                return await dbConnection.ExecuteScalarAsync<DateTime?>(query, new { codUsuarioResponsable, codGerencia });
            }
        }



        public async Task<bool> InsertarFirmaYDetalles(ImFirma firma, List<ImDetalleFirma> detallesFirma)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                await dbConnection.OpenAsync();
                using (var transaction = dbConnection.BeginTransaction())
                {
                    try
                    {
                        // Obtener la fecha de la última firma del mes actual (solo como referencia)
                        var ultimaFirma = await ObtenerUltimaFirmaDelMes(firma.CodUsuarioResponsable, firma.CodGerencia);

                        // Insertar la firma (se permite múltiples veces en el mes)
                        string queryFirma = @"
                INSERT INTO im_firma (
                    codUsuarioResponsable, 
                    fechaFirma, 
                    codGerencia, 
                    comentario, 
                    fechaCarga
                ) 
                VALUES (
                    @CodUsuarioResponsable, 
                    @FechaFirma, 
                    @CodGerencia, 
                    @Comentario, 
                    @FechaCarga
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

                        var parameters = new DynamicParameters();
                        parameters.Add("@CodUsuarioResponsable", firma.CodUsuarioResponsable);
                        parameters.Add("@FechaFirma", firma.FechaFirma);
                        parameters.Add("@CodGerencia", firma.CodGerencia);
                        parameters.Add("@Comentario", firma.Comentario);
                        parameters.Add("@FechaCarga", detallesFirma.First().feccarga);

                        var idFirma = await dbConnection.QuerySingleAsync<int>(queryFirma, parameters, transaction);

                        // Insertar los detalles de la firma
                        string queryDetalle = @"
                INSERT INTO im_detalle_firma (
                    codFirma, pais, sistema, idPaisNegocioSistema, 
                    rutdni, dv, nombreusuario, userid, cargospr, 
                    perfil, codccosto, codccostospr, Nomccosto, 
                    cargomatriz, perfilmatriz, feccarga, cta_duplicada, Evidencia
                ) VALUES (
                    @CodFirma, @pais, @sistema, @idPaisNegocioSistema, 
                    @rutdni, @dv, @nombreusuario, @userid, @cargospr, 
                    @perfil, @codccosto, @codccostospr, @Nomccosto, 
                    @cargomatriz, @perfilmatriz, @feccarga, @cta_duplicada, @Evidencia
                )";

                        foreach (var detalle in detallesFirma)
                        {
                            detalle.CodFirma = idFirma;
                            await dbConnection.ExecuteAsync(queryDetalle, detalle, transaction);
                        }

                        transaction.Commit();
                        return true; // Indicar que la firma se insertó correctamente
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error en InsertarFirmaYDetalles: {ex.Message}", ex);
                    }
                }
            }
        }




        public async Task<IEnumerable<cl_sodimac_matriz_perfil>> ObtenerListaDataMatrizRespaldo(int IdSistema)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
					return await dbdapper.QueryAsync<cl_sodimac_matriz_perfil>(@"SELECT AL1.modulo, 
                    AL1.perfil, 
                    AL1.codPerfil,
                    AL1.Fcarga, 
                    AL1.responsable, 
                    AL1.cargoActivo, 
                    AL1.cargo, 
                    AL1.idNivelCargo, 
                    AL1.idPaisNegocioSistema, 
                    AL1.idUsuario, 
                    AL1.estado_matriz, 
                    AL1.matriz, 
                    AL1.usocargo, 
                    AL1.tipocarga, 
                    AL1.nivelcargo 
                    FROM dbo.im_sodimac_matriz_perfil_respaldo AL1 
                    WHERE (AL1.idPaisNegocioSistema=@IdSistema)", new { IdSistema });

			}
		}

		public async Task<IEnumerable<Abm_Sistema>> ObtenerListaSistemaManual()
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryAsync<Abm_Sistema>(@"SELECT AL3.sistema, 
                AL2.idPaisNegocioSistema as idSistema 
                FROM dbo.pais AL1,
                dbo.pais_negocio_sistema AL2, 
                dbo.sistema AL3, dbo.pnsjt AL4 
                WHERE (AL2.idPais=AL1.idPais AND
                AL3.idSistema=AL2.idSistema AND
                AL4.idPaisNegocioSistema=AL2.idPaisNegocioSistema)
                AND ((AL1.pais='IMPERIAL' AND AL4.tipocarga='Manual'))");
			}
		}



		public async Task<Abm_Sistema> ObtenerSistemaManualPorId(int Idsistema)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				return await dbdapper.QueryFirstOrDefaultAsync<Abm_Sistema>(@"
            SELECT AL3.sistema, 
                   AL2.idPaisNegocioSistema as idSistema 
            FROM dbo.pais AL1
            INNER JOIN dbo.pais_negocio_sistema AL2 
                ON AL2.idPais = AL1.idPais
            INNER JOIN dbo.sistema AL3 
                ON AL3.idSistema = AL2.idSistema
            INNER JOIN dbo.pnsjt AL4 
                ON AL4.idPaisNegocioSistema = AL2.idPaisNegocioSistema
            WHERE AL1.pais = 'IMPERIAL' 
              AND AL4.tipocarga = 'Manual' 
              AND AL2.idPaisNegocioSistema = @Idsistema",
					new { Idsistema });
			}
		}

        // feccarga
        public async Task<DateTime?> ObtenerUltimaFechaDeCarga()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Consulta SQL que obtiene la última fecha de carga
                var query = @"
                SELECT TOP 1 feccarga
                FROM im_agrupa_activos
                ORDER BY feccarga DESC";


                return await dbdapper.QueryFirstOrDefaultAsync<DateTime?>(query);
            }
        }





        //  fin feccarga


        public async Task RegistrarDatosMatrizPerfil(IEnumerable<cl_sodimac_matriz_perfil> dataAnterior,
			int IdSistema, List<cl_sodimac_matriz_perfil> dataNueva)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				string tablarespaldo = "";
				string tablaoficial = "";

					tablarespaldo = "im_sodimac_matriz_perfil_respaldo";
					tablaoficial = "im_sodimac_matriz_perfil";

				await dbdapper.ExecuteAsync(@"DELETE FROM " + tablarespaldo + @"
                    WHERE idPaisNegocioSistema = @IdSistema", new { IdSistema });
				foreach (var item in dataAnterior)
				{
					await dbdapper.ExecuteAsync(@"INSERT INTO " + tablarespaldo + @"
                           (modulo 
                           ,perfil 
                           ,codPerfil 
                           ,Fcarga 
                           ,responsable 
                           ,cargoActivo 
                           ,cargo 
                           ,idNivelCargo 
                           ,idPaisNegocioSistema 
                           ,idUsuario 
                           ,estado_matriz 
                           ,matriz 
                           ,usocargo 
                           ,tipocarga 
                           ,nivelcargo)
                     VALUES
                           (@modulo
                           ,@perfil
                           ,@codPerfil
                           ,@Fcarga
                           ,@responsable
                           ,@cargoActivo
                           ,@cargo
                           ,@idNivelCargo
                           ,@idPaisNegocioSistema
                           ,@idUsuario
                           ,@estado_matriz
                           ,@matriz 
                           ,@usocargo
                           ,@tipocarga
                           ,@nivelcargo)", item);
				}

				await dbdapper.ExecuteAsync(@"DELETE FROM " + tablaoficial + @"
                WHERE idPaisNegocioSistema = @IdSistema", new { IdSistema });

				foreach (var item in dataNueva)
				{
					await dbdapper.ExecuteAsync(@"INSERT INTO " + tablaoficial + @"
                           (modulo 
                           ,perfil 
                           ,codPerfil 
                           ,Fcarga 
                           ,responsable 
                           ,cargoActivo 
                           ,cargo 
                           ,idNivelCargo 
                           ,idPaisNegocioSistema 
                           ,idUsuario 
                           ,estado_matriz 
                           ,matriz 
                           ,usocargo 
                           ,tipocarga 
                           ,nivelcargo)
                     VALUES
                           (@modulo
                           ,@perfil
                           ,@codPerfil
                           ,@Fcarga
                           ,@responsable
                           ,@cargoActivo
                           ,@cargo
                           ,@idNivelCargo
                           ,@idPaisNegocioSistema
                           ,@idUsuario
                           ,@estado_matriz
                           ,@matriz 
                           ,@usocargo
                           ,@tipocarga
                           ,@nivelcargo)", item);
				}
			}
		}

	}
}

