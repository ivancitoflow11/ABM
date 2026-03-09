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
                // AJUSTE: Usamos COALESCE(RUT, ID, NOMBRE) para identificar usuarios únicos.
                return await dbdapper.QueryAsync<EstadisticasUsuarios>(@"
            WITH ActivosData AS (
                SELECT 
                    AL1.PAIS AS Pais,
                    AL1.NEGOCIO AS Negocio,
                    AL1.VERTICAL AS Vertical,
                    AL1.SISTEMA AS Sistema,
                    AL1.BANDERA AS Bandera,
                    -- AQUI ESTA EL CAMBIO: COALESCE busca el primero que tenga datos (RUT -> ID -> NOMBRE)
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'ACTIVO' THEN COALESCE(AL1.RUT_DNI, AL1.ID_USUARIO, AL1.NOMBRE) END) AS Activos,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'FINIQUITADO' THEN COALESCE(AL1.RUT_DNI, AL1.ID_USUARIO, AL1.NOMBRE) END) AS Finiquitados,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'NO ENCONTRADO' THEN COALESCE(AL1.RUT_DNI, AL1.ID_USUARIO, AL1.NOMBRE) END) AS No_Encontrados,
                    COUNT(DISTINCT CASE WHEN AL1.estado = 'CUENTA DUPLICADA' THEN COALESCE(AL1.RUT_DNI, AL1.ID_USUARIO, AL1.NOMBRE) END) AS CtaDuplicadas,
                    -- Total general con la misma lógica
                    COUNT(DISTINCT CASE WHEN AL1.estado IN ('ACTIVO', 'FINIQUITADO', 'NO ENCONTRADO') OR AL1.estado = 'CUENTA DUPLICADA' THEN COALESCE(AL1.RUT_DNI, AL1.ID_USUARIO, AL1.NOMBRE) END) AS total_Usuarios
                FROM (
                    SELECT 
                        b.PAIS, 
                        a.IDNEGOCIO, 
                        a.NEGOCIO, 
                        a.VERTICAL, 
                        a.IDSISTEMA, 
                        a.SISTEMA, 
                        a.NOMBRE, 
                        a.RUT_DNI, 
                        a.ID_USUARIO, 
                        a.CARGO, 
                        a.C_COSTO, 
                        a.FECHA_FINIQUITO, 
                        a.ALTA, 
                        a.BAJA, 
                        a.ESTADO, 
                        b.BANDERA
                    FROM ftc_agrupa_activos_app a 
                    INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] pns ON a.IDSISTEMA = pns.idSistema AND a.IDNEGOCIO = pns.idNegocio
                    LEFT OUTER JOIN ftc_pais b ON a.pais = b.pais OR a.pais = b.codPais 
                ) AL1
                WHERE (@idNegocio IS NULL OR AL1.IDNEGOCIO = @idNegocio)
                  AND (@idSistema IS NULL OR AL1.IDSISTEMA = @idSistema)
                GROUP BY AL1.PAIS, AL1.NEGOCIO, AL1.VERTICAL, AL1.SISTEMA, AL1.BANDERA
            ),
            GestionDiariaData AS (
                SELECT DISTINCT 
                    ISNULL(b.sistema, 'SIN SISTEMA') AS Sistema,
                    ISNULL(b.negocio,'SIN NEGOCIO') AS Negocio,
                    ISNULL(b.pais,'SIN PAIS') AS Pais,
                    ISNULL(ftc_gestion_diaria.cnt_recontratados, 0) AS Recontratados,
                    ISNULL(ftc_gestion_diaria.entre_1_3, 0) AS De_1_a_3_Dias_Sin_Gestion,
                    ISNULL(ftc_gestion_diaria.entre_4_6, 0) AS De_4_a_6_Dias_Sin_Gestion,
                    ISNULL(ftc_gestion_diaria.mayor_a_6, 0) AS Mas_de_6_Dias_Sin_Gestion
                FROM ftc_gestion_diaria 
                INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] b ON ftc_gestion_diaria.idPaisNegocioSistema = b.idPaisNegocioSistema
                WHERE 
                    ftc_gestion_diaria.feccarga = (
                        SELECT TOP 1 feccarga 
                        FROM ftc_gestion_diaria 
                        ORDER BY SUBSTRING(feccarga,7,4)+SUBSTRING(feccarga,4,2)+SUBSTRING(feccarga,1,2) DESC
                    )
                    AND (@idNegocio IS NULL OR B.idNegocio = @idNegocio)
                    AND (@idSistema IS NULL OR B.idSistema = @idSistema)
            )

            SELECT 
                A.Sistema,
                A.Negocio,
                A.Pais,
                A.Vertical, 
                A.Bandera,
                A.total_Usuarios AS TotalUsuarios,
                A.Activos,
                A.Finiquitados,
                A.No_Encontrados AS NoEncontrados,
                A.CtaDuplicadas,
                ISNULL(G.Recontratados, 0) AS Recontratados,
                ISNULL(G.De_1_a_3_Dias_Sin_Gestion, 0) AS De1a3DiasSinGestion,
                ISNULL(G.De_4_a_6_Dias_Sin_Gestion, 0) AS De4a6DiasSinGestion,
                ISNULL(G.Mas_de_6_Dias_Sin_Gestion, 0) AS MasDe6DiasSinGestion
            FROM ActivosData A
            LEFT JOIN GestionDiariaData G 
                ON A.Sistema = G.Sistema AND A.Negocio = G.Negocio AND A.Pais = G.Pais;
        ", new { idNegocio, idSistema }, commandTimeout: 1800);
            }
        }

        public async Task<IEnumerable<Finiquitados>> ObtenerDetalleFiniquitados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // AJUSTE QUERY 2: INNER JOIN con PNS_2
                return await dbdapper.QueryAsync<Finiquitados>(@"
            SELECT 
                b.PAIS          AS pais,
                a.NEGOCIO       AS negocio,
                a.VERTICAL      AS Vertical,
                a.SISTEMA       AS sistema,
                a.NOMBRE        AS nombreusuario,
                a.RUT_DNI       AS rutdni,
                a.ID_USUARIO    AS userid,
                a.CARGO         AS cargo,
                a.C_COSTO       AS codccostospr,
                a.FECHA_FINIQUITO AS fecfiniq,
                a.ALTA          AS fecalta,
                a.BAJA          AS fecbaja
            FROM ftc_agrupa_activos_app a 
            -- CRUCE OBLIGATORIO
            INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] pns ON a.IDSISTEMA = pns.idSistema AND a.IDNEGOCIO = pns.idNegocio
            LEFT OUTER JOIN ftc_pais b ON a.pais = b.pais OR a.pais = b.codPais 
            WHERE a.ESTADO = 'FINIQUITADO'
                AND (@idNegocio IS NULL OR a.IDNEGOCIO = @idNegocio)
                AND (@idSistema IS NULL OR a.IDSISTEMA = @idSistema);
        ", new { idNegocio, idSistema }, commandTimeout: 1800);
            }
        }

        public async Task<IEnumerable<UsuariosNoEncontrados>> ObtenerDetalleNoEncontrados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // AJUSTE QUERY 3: INNER JOIN con PNS_2
                return await dbdapper.QueryAsync<UsuariosNoEncontrados>(@"
            SELECT 
                b.PAIS          AS pais,
                a.NEGOCIO       AS negocio,
                a.VERTICAL      AS Vertical,
                a.SISTEMA       AS sistema,
                a.NOMBRE        AS nombreusuario,
                a.RUT_DNI       AS rutdni,
                a.ID_USUARIO    AS userid,
                a.CARGO         AS cargo,
                a.C_COSTO       AS Nomccostospr,
                a.ALTA          AS fecalta,
                a.BAJA          AS fecbaja
            FROM ftc_agrupa_activos_app a 
            -- CRUCE OBLIGATORIO
            INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] pns ON a.IDSISTEMA = pns.idSistema AND a.IDNEGOCIO = pns.idNegocio
            LEFT OUTER JOIN ftc_pais b ON a.pais = b.pais OR a.pais = b.codPais 
            WHERE a.ESTADO = 'NO ENCONTRADO'
                AND (@idNegocio IS NULL OR a.IDNEGOCIO = @idNegocio)
                AND (@idSistema IS NULL OR a.IDSISTEMA = @idSistema);
        ", new { idNegocio, idSistema }, commandTimeout: 1800);
            }
        }

        public async Task<IEnumerable<UsuariosDuplicados>> ObtenerDetalleDuplicados(int? idNegocio, int? idSistema)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // AJUSTE QUERY 4: INNER JOIN con PNS_2 dentro del CTE
                return await dbdapper.QueryAsync<UsuariosDuplicados>(@"
            WITH CTE_Cuentas AS (
                SELECT 
                    b.PAIS          AS pais,
                    a.NEGOCIO       AS negocio,
                    a.VERTICAL      AS Vertical,
                    a.SISTEMA       AS sistema,
                    a.NOMBRE        AS nombreusuario,
                    a.RUT_DNI       AS rutdni,
                    a.ID_USUARIO    AS userid,
                    a.CARGO         AS cargo,
                    a.C_COSTO       AS codccosto,
                    a.FECHA_FINIQUITO AS fecfiniq,
                    a.ALTA          AS fecalta,
                    a.BAJA          AS fecbaja,
                    ROW_NUMBER() OVER (PARTITION BY a.RUT_DNI ORDER BY a.BAJA DESC) AS RowNum 
                FROM ftc_agrupa_activos_app a 
                -- CRUCE OBLIGATORIO
                INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] pns ON a.IDSISTEMA = pns.idSistema AND a.IDNEGOCIO = pns.idNegocio
                LEFT OUTER JOIN ftc_pais b ON a.pais = b.pais OR a.pais = b.codPais 
                WHERE a.ESTADO = 'CUENTA DUPLICADA'
                    AND (@idNegocio IS NULL OR a.IDNEGOCIO = @idNegocio)
                    AND (@idSistema IS NULL OR a.IDSISTEMA = @idSistema)
            )
            SELECT *
            FROM CTE_Cuentas
            WHERE RowNum = 1;
        ", new { idNegocio, idSistema }, commandTimeout: 1800);
            }
        }

        public async Task<IEnumerable<Sistema>> ObtenerSistemasPorNegocio(int? idNegocio)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // QUERY 5: Esta ya estaba usando FTC_PNS_2, se mantiene correcta.
                var query = @"
            SELECT DISTINCT S.idSistema, S.codSistema, S.sistema
            FROM FTC_PNS_2 S
            WHERE (@idNegocio IS NULL OR S.idNegocio = @idNegocio)
              AND S.idSistema NOT IN (3,110)
            GROUP BY S.idSistema, S.codSistema, S.sistema
            ORDER BY S.sistema";

                return await dbdapper.QueryAsync<Sistema>(query, new { idNegocio }, commandTimeout: 1800);
            }
        }
    }
}