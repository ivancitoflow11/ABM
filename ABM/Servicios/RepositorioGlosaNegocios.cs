using ABM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioGlosaNegocios
    {
        Task CrearAsync(GlosaNegocioModel glosaNegocio);
        Task<IEnumerable<GlosaNegocioModel>> ObtenerTodosAsync();
        Task<GlosaNegocioModel> ObtenerPorGlosaAsync(string glosa);
        Task<bool> ActualizarAsync(string glosaOriginal, GlosaNegocioModel glosaNegocio);
        Task<bool> EliminarAsync(string glosa);

    }

    public class RepositorioGlosaNegocios : IRepositorioGlosaNegocios
    {
        private readonly string connectionString;

        public RepositorioGlosaNegocios(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }


        public async Task CrearAsync(GlosaNegocioModel glosaNegocio)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"INSERT INTO ftc_glosa_negocios (GLOSA, NEGOCIO, PAIS) 
                              VALUES (@GLOSA, @NEGOCIO, @PAIS);";
                await db.ExecuteAsync(query, glosaNegocio);
            }
        }

        public async Task<IEnumerable<GlosaNegocioModel>> ObtenerTodosAsync()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = "SELECT GLOSA, NEGOCIO, PAIS FROM ftc_glosa_negocios ORDER BY GLOSA;";
                return await db.QueryAsync<GlosaNegocioModel>(query);
            }
        }

        public async Task<GlosaNegocioModel> ObtenerPorGlosaAsync(string glosa)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Usar ltrim(rtrim()) como en la query de eliminación para consistencia
                var query = @"SELECT GLOSA, NEGOCIO, PAIS FROM ftc_glosa_negocios 
                              WHERE ltrim(rtrim(GLOSA)) = ltrim(rtrim(@GlosaParam));";
                return await db.QueryFirstOrDefaultAsync<GlosaNegocioModel>(query, new { GlosaParam = glosa });
            }
        }

        public async Task<bool> ActualizarAsync(string glosaOriginal, GlosaNegocioModel glosaNegocio)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"UPDATE ftc_glosa_negocios 
                                SET GLOSA = @GLOSA, NEGOCIO = @NEGOCIO, PAIS = @PAIS
                                WHERE ltrim(rtrim(GLOSA)) = ltrim(rtrim(@GlosaOriginalParam));";
                var parameters = new
                {
                    GLOSA = glosaNegocio.GLOSA,
                    NEGOCIO = glosaNegocio.NEGOCIO,
                    PAIS = glosaNegocio.PAIS,
                    GlosaOriginalParam = glosaOriginal
                };
                var affectedRows = await db.ExecuteAsync(query, parameters);
                return affectedRows > 0;
            }
        }

        public async Task<bool> EliminarAsync(string glosa)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"DELETE FROM ftc_glosa_negocios 
                                WHERE ltrim(rtrim(GLOSA)) = ltrim(rtrim(@GlosaParam));";
                var affectedRows = await db.ExecuteAsync(query, new { GlosaParam = glosa });
                return affectedRows > 0;
            }
        }


    }
}