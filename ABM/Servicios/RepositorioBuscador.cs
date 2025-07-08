using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace ABM.Servicios
{

    public interface IRepositorioBuscador
    {
        Task<IEnumerable<ActivosFalanetUser>> BuscarEnActivosFalanet(string rut, string apePaterno, string correo);
        Task<IEnumerable<AdUser>> BuscarUsuariosEnAd(string employeeId, string displayName, string mail);

    }

    public class RepositorioBuscador : IRepositorioBuscador
    {
        private readonly string connectionString;

        public RepositorioBuscador(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        // --- Búsqueda en Active Directory (Fase 1) ---
        public async Task<IEnumerable<AdUser>> BuscarUsuariosEnAd(string employeeId, string displayName, string mail)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT
                employeeID,
                cn AS DisplayName, -- CAMBIO: Se obtiene 'cn' y se asigna a la propiedad DisplayName
                mail,
                sAMAccountName,
                title,
                department,
                company,
                userAccountControl,
                c,
                wWWHomePage
            FROM
                dbo.ftc_ad
            WHERE
                -- Aplicar el filtro de ID con igualdad (mucho más rápido) si no está vacío.
                (@employeeId IS NOT NULL AND employeeID = @employeeId)

                -- O aplicar el filtro de nombre (usando la columna 'cn') si no está vacío.
                OR (@displayName IS NOT NULL AND cn LIKE '%' + @displayName + '%') -- CAMBIO: Se busca en la columna 'cn'

                -- O aplicar el filtro de correo si no está vacío.
                OR (@mail IS NOT NULL AND mail LIKE '%' + @mail + '%');";

                return await db.QueryAsync<AdUser>(sql, new
                {
                    employeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId,
                    displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                    mail = string.IsNullOrWhiteSpace(mail) ? null : mail
                });
            }
        }
        // --- Implementación Fase 2 (AÑADIR ESTE MÉTODO) ---
        public async Task<IEnumerable<ActivosFalanetUser>> BuscarEnActivosFalanet(string rut, string apePaterno, string correo)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT DISTINCT -- >> CAMBIO AQUÍ <<
                [PAÍS], [RUT], [D_VERIFICADOR], [NUMUSER], [RUT_SUPERIOR], [APEPATERNO],
                [APEMATERNO], [NOMBRES], [CTAUSUARIO], [CORREO], [CODEMPRESA], [RAZSOCIAL],
                [NOMEMPRESA], [RUTEMPRESA], [DIGVERRUT_EMP], [GRUPOEMPRESA], [CODCARGO],
                [NOMCARGO], [CCOSTO], [FECHACARGA]
            FROM
                [dbo].[ftc_activos_falanet]
            WHERE
                (@rut IS NOT NULL AND RUT = @rut)
                OR (@apePaterno IS NOT NULL AND APEPATERNO LIKE '%' + @apePaterno + '%')
                OR (@correo IS NOT NULL AND CORREO = @correo);";

                return await db.QueryAsync<ActivosFalanetUser>(sql, new
                {
                    rut = string.IsNullOrWhiteSpace(rut) ? null : rut,
                    apePaterno = string.IsNullOrWhiteSpace(apePaterno) ? null : apePaterno,
                    correo = string.IsNullOrWhiteSpace(correo) ? null : correo
                });
            }
        }

    }
}