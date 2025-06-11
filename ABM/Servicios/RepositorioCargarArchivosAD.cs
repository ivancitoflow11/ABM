
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    public interface IRepositorioCargarArchivosAD
    {
        // Métodos para obtener datos para los dropdowns
        Task<IEnumerable<string>> ObtenerPaisesUnicosAsync();
        Task<IEnumerable<string>> ObtenerNegociosPorPaisAsync(string pais);

        // Método para obtener el ID del cruce
        Task<int?> ObtenerIdCruceKpiAsync(string pais, string negocio);

        // Métodos para la tabla de históricos
        Task<int> CrearHistoricoCargaAsync(HistoricoCargaKPI historico);
        Task<IEnumerable<HistoricoCargaKPI>> ObtenerHistoricoCargasAsync();
    }
    public class RepositorioCargarArchivosAD : IRepositorioCargarArchivosAD
    {
        private readonly string connectionString;

        public RepositorioCargarArchivosAD(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<string>> ObtenerPaisesUnicosAsync()
        {
            using var db = new SqlConnection(connectionString);
            var sql = "SELECT DISTINCT Pais FROM ftc_cruces_kpi ORDER BY Pais;";
            return await db.QueryAsync<string>(sql);
        }

        public async Task<IEnumerable<string>> ObtenerNegociosPorPaisAsync(string pais)
        {
            using var db = new SqlConnection(connectionString);
            var sql = "SELECT DISTINCT Negocio FROM ftc_cruces_kpi WHERE Pais = @Pais ORDER BY Negocio;";
            return await db.QueryAsync<string>(sql, new { Pais = pais });
        }

        public async Task<int?> ObtenerIdCruceKpiAsync(string pais, string negocio)
        {
            using var db = new SqlConnection(connectionString);
            var sql = "SELECT TOP 1 IdCrucesKPI FROM ftc_cruces_kpi WHERE Pais = @Pais AND Negocio = @Negocio;";
            return await db.QuerySingleOrDefaultAsync<int?>(sql, new { Pais = pais, Negocio = negocio });
        }

        public async Task<int> CrearHistoricoCargaAsync(HistoricoCargaKPI historico)
        {
            using var db = new SqlConnection(connectionString);
            // El IdCarga es identity, por lo que no se incluye en el INSERT
            var sql = @"
                INSERT INTO ftc_historico_carga_kpi (IdCrucesKPI, Archivo, Fecha)
                VALUES (@IdCrucesKPI, @Archivo, @Fecha);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await db.ExecuteScalarAsync<int>(sql, historico);
        }

        public async Task<IEnumerable<HistoricoCargaKPI>> ObtenerHistoricoCargasAsync()
        {
            using var db = new SqlConnection(connectionString);
            // Hacemos un JOIN para obtener los nombres de País y Negocio para mostrarlos en la lista
            var sql = @"
                SELECT
                    h.IdCarga,
                    h.IdCrucesKPI,
                    h.Archivo,
                    h.Fecha,
                    c.Pais AS NombrePais,
                    c.Negocio AS NombreNegocio
                FROM ftc_historico_carga_kpi h
                INNER JOIN ftc_cruces_kpi c ON h.IdCrucesKPI = c.IdCrucesKPI
                ORDER BY h.Fecha DESC, h.IdCarga DESC;";
            return await db.QueryAsync<HistoricoCargaKPI>(sql);
        }
    }
}