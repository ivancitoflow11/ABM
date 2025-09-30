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
        Task<IEnumerable<EstadoUsuario>> ObtenerEstados(string input);
    }

    public class RepositorioConsultaUsuario : IRepositorioConsultaUsuario
    {
        private readonly string _connectionString;

        public RepositorioConsultaUsuario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<DatosBasicosUsuario?> ObtenerDatosBasicos(string input)
        {
            using var db = new SqlConnection(_connectionString);
            // El SP ya quedó con @input en el WHERE (como me mostraste)
            return await db.QueryFirstOrDefaultAsync<DatosBasicosUsuario>(
                "CSS_DatosBasicosUsuario",
                new { input },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<EstadoUsuario>> ObtenerEstados(string input)
        {
            using var db = new SqlConnection(_connectionString);

            var estados = new List<EstadoUsuario>();

            // SPR / Activos Falanet
            var sprRow = await db.QueryFirstOrDefaultAsync(
                "CSS_DatosEstadoActivo",
                new { input },
                commandType: CommandType.StoredProcedure);
            if (sprRow != null)
                estados.Add(new EstadoUsuario { NombreEstado = "SPR", Valor = "ACTIVO" });

            // AD
            var adRow = await db.QueryFirstOrDefaultAsync(
                "CSS_DatosEstadoAD",
                new { input },
                commandType: CommandType.StoredProcedure);
            if (adRow != null)
                estados.Add(new EstadoUsuario { NombreEstado = "AD", Valor = "ACTIVO" });

            // FINIQUITADO
            var finRow = await db.QueryFirstOrDefaultAsync(
                "CSS_DatosEstadoFiniquitado",
                new { input },
                commandType: CommandType.StoredProcedure);
            if (finRow != null)
                estados.Add(new EstadoUsuario { NombreEstado = "FINIQUITADO", Valor = "SI" });

            return estados;
        }
    }
}
