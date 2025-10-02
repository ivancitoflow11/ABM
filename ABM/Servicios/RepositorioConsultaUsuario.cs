using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    public interface IRepositorioConsultaUsuario
    {
        Task<DatosBasicosUsuario?> ObtenerDatosBasicos(string input);
        Task<IEnumerable<DatosBasicosUsuario>> BuscarCoincidencias(string input);
        Task<bool> ExisteEnADPorMailORut(string correo, string rut); 
        Task<bool> EstaFiniquitado(string input);
        Task<(bool spr, bool empCentral)> ObtenerEstadoSprEmpCentral(string input);
    }

    public class RepositorioConsultaUsuario : IRepositorioConsultaUsuario
    {
        private readonly string _connectionString;

        public RepositorioConsultaUsuario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<(bool spr, bool empCentral)> ObtenerEstadoSprEmpCentral(string input)
        {
            using var db = new SqlConnection(_connectionString);

            // El SP devuelve filas; ORIGEN = 'SPR' o 'Empleado Central'
            var filas = await db.QueryAsync(
                "CSS_DatosEstadoActivo",
                new { input },
                commandType: CommandType.StoredProcedure
            );

            bool spr = false, emp = false;
            foreach (var f in filas)
            {
                var origen = (string)(f.ORIGEN ?? string.Empty);
                if (origen.Equals("SPR", StringComparison.OrdinalIgnoreCase)) spr = true;
                else if (origen.Equals("Empleado Central", StringComparison.OrdinalIgnoreCase)) emp = true;
                if (spr && emp) break; // ya tenemos ambos
            }

            return (spr, emp);
        }

        public async Task<bool> EstaFiniquitado(string input)
        {
            using var db = new SqlConnection(_connectionString);
            // El SP devuelve una fila (RUTDNI) si está finiquitado
            var rut = await db.QueryFirstOrDefaultAsync<string>(
                "CSS_DatosEstadoFiniquitado",
                new { input },
                commandType: CommandType.StoredProcedure
            );
            return !string.IsNullOrEmpty(rut);
        }
        // SP TOP 1 con RUT (ya lo tienes)
        public async Task<DatosBasicosUsuario?> ObtenerDatosBasicos(string input)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryFirstOrDefaultAsync<DatosBasicosUsuario>(
                "CSS_DatosBasicosUsuario",
                new { input },
                commandType: CommandType.StoredProcedure);
        }

        // Autocomplete: TOP 10 + relevancia + concatenado + SIN TILDES
        public async Task<IEnumerable<DatosBasicosUsuario>> BuscarCoincidencias(string input)
        {
            using var db = new SqlConnection(_connectionString);

            // IMPORTANTE: ajusta el nombre de collation si tu servidor usa otro;
            // Latin1_General_CI_AI = Case-Insensitive, Accent-Insensitive
            var sql = @"
                SELECT TOP 10
                       (NOMBRES + ' ' + APEPATERNO + ' ' + APEMATERNO) AS Nombre,
                       CORREO,
                       RUT
                FROM ftc_activos_falanet
                WHERE (
                        (NOMBRES + ' ' + APEPATERNO + ' ' + APEMATERNO) COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                     OR NOMBRES    COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                     OR APEPATERNO COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                     OR APEMATERNO COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                     OR CORREO     COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                     OR RUT        COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
                )
                GROUP BY NOMBRES, APEPATERNO, APEMATERNO, CORREO, RUT
                ORDER BY 
                    CASE 
                        WHEN NOMBRES    COLLATE Latin1_General_CI_AI LIKE @input + '%' COLLATE Latin1_General_CI_AI THEN 1
                        WHEN APEPATERNO COLLATE Latin1_General_CI_AI LIKE @input + '%' COLLATE Latin1_General_CI_AI THEN 2
                        WHEN APEMATERNO COLLATE Latin1_General_CI_AI LIKE @input + '%' COLLATE Latin1_General_CI_AI THEN 3
                        ELSE 4
                    END,
                    NOMBRES, APEPATERNO, APEMATERNO;";

            return await db.QueryAsync<DatosBasicosUsuario>(sql, new { input });
        }

        // Lookup AD exacto (recomendado para la card AD)
        public async Task<bool> ExisteEnADPorMailORut(string correo, string rut)
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"
                SELECT TOP 1 1
                FROM ftc_ad
                WHERE (MAIL = @correo AND @correo IS NOT NULL AND @correo <> '')
                   OR (employeeID = @rut AND @rut IS NOT NULL AND @rut <> '')";
            var existe = await db.ExecuteScalarAsync<int?>(sql, new { correo, rut });
            return existe.HasValue;
        }
    }
}
