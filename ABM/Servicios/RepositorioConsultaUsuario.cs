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
        Task<DatosAD?> ObtenerDatosAD(string correo, string rut);
        Task<DatosFiniquito?> ObtenerDatosFiniquito(string input);
        Task<(bool spr, bool empCentral)> ObtenerEstadoSprEmpCentral(string input);
        Task<IEnumerable<SistemaUsuario>> ObtenerSistemas(string input);
    }

    public class RepositorioConsultaUsuario : IRepositorioConsultaUsuario
    {
        private readonly string _connectionString;

        public RepositorioConsultaUsuario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<SistemaUsuario>> ObtenerSistemas(string input)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<SistemaUsuario>(
                "CSS_DatosSistemas",
                new { input },
                commandType: CommandType.StoredProcedure
            );
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
        public async Task<DatosAD?> ObtenerDatosAD(string correo, string rut)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryFirstOrDefaultAsync<DatosAD>(
                "CSS_DatosUltimoLoginAD",
                new { correo, rut },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<DatosFiniquito?> ObtenerDatosFiniquito(string input)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryFirstOrDefaultAsync<DatosFiniquito>(
                "CSS_DatosEstadoFiniquitado",
                new { input },
                commandType: CommandType.StoredProcedure
            );
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

            // Ahora llamamos directamente al Procedimiento Almacenado.
            // Dapper mapeará las columnas del SP a las propiedades de 'DatosBasicosUsuario'.
            return await db.QueryAsync<DatosBasicosUsuario>(
                "CSS_DatosBasicosUsuario",      // Nombre exacto del SP
                new { input },                  // Parámetros
                commandType: CommandType.StoredProcedure // Indicamos que es un SP
            );
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
