using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration; // Agregado para IConfiguration

namespace ABM.Servicios
{
    public interface IRepositorioBuscador
    {
        Task<IEnumerable<ActivosFalanetUser>> BuscarEnActivosFalanet(string rut, string nombreCompleto, string correo);
        Task<IEnumerable<AdUser>> BuscarUsuariosEnAd(string employeeId, string displayName, string mail);
        Task<IEnumerable<FiniquitadoUser>> BuscarEnFiniquitados(string rutDni, string nombreUsuario, string mailUsuario, int idPais, int idNegocio);
        Task<IEnumerable<SapUser>> BuscarEnPasoSap(string rutODni, string nombreCompleto, string correoUsuario);
        Task<IEnumerable<AgrupaActivosUser>> BuscarEnAgrupaActivos(string rutDni, string nombreUsuario, string mailUsuario);
    }

    public class RepositorioBuscador : IRepositorioBuscador
    {
        private readonly string connectionString;

        public RepositorioBuscador(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<SapUser>> BuscarEnPasoSap(string rutODni, string nombreCompleto, string correoUsuario)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // NOTA: ftc_paso_sap parece ser data cruda sin IDs de sistema para cruzar con ftc_pns_2.
                // Se mantiene igual a menos que existan columnas idSistema/idNegocio.
                var sql = @"
                    SELECT DISTINCT
                        Pais,
                        [RUT_o_DNI] AS RutODni,
                        [Dig._Verif.] AS DigVerif,
                        CONCAT(Nombres, ' ', Apellido_Paterno, ' ', Apellido_Materno) AS NombreCompleto,
                        Correo_Usuario AS CorreoUsuario,
                        [User_ID] as UserId,
                        Cargo,
                        Sistema,
                        Negocio,
                        [Fecha_Último_Login] as FechaUltimoLogin
                    FROM
                        [dbo].[ftc_paso_sap]
                    WHERE
                        (@rutODni IS NOT NULL AND [RUT_o_DNI] = @rutODni)
                        OR (@nombreCompleto IS NOT NULL AND CONCAT(Nombres, ' ', Apellido_Paterno, ' ', Apellido_Materno) LIKE '%' + @nombreCompleto + '%')
                        OR (@correoUsuario IS NOT NULL AND Correo_Usuario = @correoUsuario);";

                return await db.QueryAsync<SapUser>(sql, new
                {
                    rutODni = string.IsNullOrWhiteSpace(rutODni) ? null : rutODni,
                    nombreCompleto = string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto,
                    correoUsuario = string.IsNullOrWhiteSpace(correoUsuario) ? null : correoUsuario
                });
            }
        }

        public async Task<IEnumerable<AgrupaActivosUser>> BuscarEnAgrupaActivos(string rutDni, string nombreUsuario, string mailUsuario)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
                    SELECT DISTINCT
                        rutdni AS RutDni,
                        dv,
                        nombreusuario AS NombreUsuario,
                        mailusuario AS MailUsuario,
                        cargospr AS CargoSpr,
                        empresa,
                        fecultlogin AS FecUltLogin,
                        estado AS Estado
                    FROM
                        [dbo].[ftc_agrupa_activos]
                    WHERE
                        (@rutDni IS NOT NULL AND rutdni = @rutDni)
                        OR (@nombreUsuario IS NOT NULL AND nombreusuario LIKE '%' + @nombreUsuario + '%')
                        OR (@mailUsuario IS NOT NULL AND mailusuario = @mailUsuario);";

                return await db.QueryAsync<AgrupaActivosUser>(sql, new
                {
                    rutDni = string.IsNullOrWhiteSpace(rutDni) ? null : rutDni,
                    nombreUsuario = string.IsNullOrWhiteSpace(nombreUsuario) ? null : nombreUsuario,
                    mailUsuario = string.IsNullOrWhiteSpace(mailUsuario) ? null : mailUsuario
                });
            }
        }

        public async Task<IEnumerable<FiniquitadoUser>> BuscarEnFiniquitados(string rutDni, string nombreUsuario, string mailUsuario, int idPais, int idNegocio)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // --- CAMBIO IMPORTANTE AQUÍ ---
                // Reemplazamos el cruce con ftc_pais_negocio_sistema (antigua) por ftc_pns_2 (nueva)
                // Y usamos los campos correctos de la nueva tabla.
                var sql = @"
                    SELECT DISTINCT
                        AL1.rutdni,
                        AL1.dv,
                        AL1.nombreusuario,
                        AL1.mailusuario,
                        AL1.cargospr,
                        AL2.sistema,  -- Viene de la PNS_2
                        AL2.pais,     -- Viene de la PNS_2
                        AL2.negocio,  -- Viene de la PNS_2
                        AL1.fecfiniq,
                        G.Nom_Gerencia
                    FROM
                        dbo.ftc_agrupa_activos AL1
                    JOIN
                        -- CAMBIO: Usamos la nueva tabla maestra de sistemas activos
                        dbo.ftc_pns_2 AL2 ON AL2.idPaisNegocioSistema = AL1.idPaisNegocioSistema
                    LEFT JOIN
                        dbo.ftc_Subgerencias S ON AL1.Nomccostospr = S.Nom_Subgerencia
                    LEFT JOIN
                        dbo.ftc_gerencia G ON S.COD_Gerencia = G.ID_gerencia
                    WHERE
                        AL1.estado = 'FINIQUITADO'
                        AND AL2.idPais = @idPais
                        AND AL2.idNegocio = @idNegocio
                        AND (
                            (@rutDni IS NOT NULL AND AL1.rutdni = @rutDni)
                            OR (@nombreUsuario IS NOT NULL AND AL1.nombreusuario LIKE '%' + @nombreUsuario + '%')
                            OR (@mailUsuario IS NOT NULL AND AL1.mailusuario = @mailUsuario)
                        )";

                return await db.QueryAsync<FiniquitadoUser>(sql, new
                {
                    rutDni = string.IsNullOrWhiteSpace(rutDni) ? null : rutDni,
                    nombreUsuario = string.IsNullOrWhiteSpace(nombreUsuario) ? null : nombreUsuario,
                    mailUsuario = string.IsNullOrWhiteSpace(mailUsuario) ? null : mailUsuario,
                    idPais,
                    idNegocio
                });
            }
        }

        public async Task<IEnumerable<AdUser>> BuscarUsuariosEnAd(string employeeId, string displayName, string mail)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT
                employeeID,
                cn AS DisplayName,
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
                (@employeeId IS NOT NULL AND employeeID = @employeeId)
                OR (@displayName IS NOT NULL AND cn LIKE '%' + @displayName + '%')
                OR (@mail IS NOT NULL AND mail LIKE '%' + @mail + '%');";

                return await db.QueryAsync<AdUser>(sql, new
                {
                    employeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId,
                    displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                    mail = string.IsNullOrWhiteSpace(mail) ? null : mail
                });
            }
        }

        public async Task<IEnumerable<ActivosFalanetUser>> BuscarEnActivosFalanet(string rut, string nombreCompleto, string correo)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT DISTINCT
                RUT,
                D_VERIFICADOR,
                CONCAT(NOMBRES, ' ', APEPATERNO, ' ', APEMATERNO) AS NombreCompleto,
                CORREO,
                NOMCARGO,
                NOMEMPRESA,
                CTAUSUARIO,
                PAÍS,
                CCOSTO
            FROM
                [dbo].[ftc_activos_falanet]
            WHERE
                (@rut IS NOT NULL AND RUT = @rut)
                OR (@nombreCompleto IS NOT NULL AND CONCAT(NOMBRES, ' ', APEPATERNO, ' ', APEMATERNO) LIKE '%' + @nombreCompleto + '%')
                OR (@correo IS NOT NULL AND CORREO = @correo);";

                return await db.QueryAsync<ActivosFalanetUser>(sql, new
                {
                    rut = string.IsNullOrWhiteSpace(rut) ? null : rut,
                    nombreCompleto = string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto,
                    correo = string.IsNullOrWhiteSpace(correo) ? null : correo
                });
            }
        }
    }
}