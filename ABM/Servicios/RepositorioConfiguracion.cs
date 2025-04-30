using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    public interface IRepositorioConfiguracion
    {

        // Métodos para paises
        Task<IEnumerable<Pais>> ObtenerPaises();
        Task<int> CrearPais(Pais nuevo);
        Task<int> ActualizarPais(Pais editado);
        Task<Pais?> ObtenerPaisPorId(int idPais);
        Task<bool> ExistePaisCodigo(string codPais, int? idPais = null);
        Task<bool> ExistePaisNombre(string nombre, int? idPais = null);

        // Métodos para sistemas
        Task<IEnumerable<Sistema>> ObtenerSistemas();
        Task<Sistema?> ObtenerSistemaPorId(int idSistema);
        Task<bool> ExisteSistemaCodigo(string codSistema, int? idSistema = null);
        Task<bool> ExisteSistemaNombre(string sistema, int? idSistema = null);
        Task<int> CrearSistema(Sistema nuevo);
        Task<int> ActualizarSistema(Sistema editado);
    }

    public class RepositorioConfiguracion : IRepositorioConfiguracion
    {
        private readonly string connectionString;

        public RepositorioConfiguracion(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        // METODOS PAISES
        public async Task<Pais?> ObtenerPaisPorId(int idPais)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT idPais, codPais, pais AS Nombre, Bandera
                  FROM ftc_pais
                 WHERE idPais = @idPais";
            return await db.QueryFirstOrDefaultAsync<Pais>(sql, new { idPais });
        }

        public async Task<bool> ExistePaisCodigo(string codPais, int? idPais = null)
        {
            using var db = new SqlConnection(connectionString);
            var sql = idPais == null
                ? "SELECT COUNT(1) FROM ftc_pais WHERE codPais = @codPais"
                : "SELECT COUNT(1) FROM ftc_pais WHERE codPais = @codPais AND idPais <> @idPais";
            var count = await db.ExecuteScalarAsync<int>(sql, new { codPais, idPais });
            return count > 0;
        }

        public async Task<bool> ExistePaisNombre(string nombre, int? idPais = null)
        {
            using var db = new SqlConnection(connectionString);
            var sql = idPais == null
                ? "SELECT COUNT(1) FROM ftc_pais WHERE pais = @nombre"
                : "SELECT COUNT(1) FROM ftc_pais WHERE pais = @nombre AND idPais <> @idPais";
            var count = await db.ExecuteScalarAsync<int>(sql, new { nombre, idPais });
            return count > 0;
        }

        public async Task<IEnumerable<Pais>> ObtenerPaises()
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT idPais, codPais, pais AS Nombre, Bandera
                  FROM ftc_pais";
            return await db.QueryAsync<Pais>(sql);
        }

        public async Task<int> CrearPais(Pais nuevo)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                INSERT INTO ftc_pais (codPais, pais, Bandera)
                VALUES (@CodPais, @Nombre, @Bandera);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await db.ExecuteScalarAsync<int>(sql, nuevo);
        }

        public async Task<int> ActualizarPais(Pais editado)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                UPDATE ftc_pais
                   SET codPais = @CodPais,
                       pais    = @Nombre,
                       Bandera = @Bandera
                 WHERE idPais = @IdPais;";
            return await db.ExecuteAsync(sql, editado);
        }
        //METODOS SISTEMAS

        public async Task<IEnumerable<Sistema>> ObtenerSistemas()
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT idSistema,
                       codSistema,
                       sistema,
                       nriesgo
                  FROM ftc_sistema";
            return await db.QueryAsync<Sistema>(sql);
        }

        public async Task<Sistema?> ObtenerSistemaPorId(int idSistema)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT idSistema,
                       codSistema,
                       sistema,
                       nriesgo
                  FROM ftc_sistema
                 WHERE idSistema = @idSistema";
            return await db.QueryFirstOrDefaultAsync<Sistema>(sql, new { idSistema });
        }

        public async Task<bool> ExisteSistemaCodigo(string codSistema, int? idSistema = null)
        {
            using var db = new SqlConnection(connectionString);
            var sql = idSistema == null
                ? "SELECT COUNT(1) FROM ftc_sistema WHERE codSistema = @codSistema"
                : "SELECT COUNT(1) FROM ftc_sistema WHERE codSistema = @codSistema AND idSistema <> @idSistema";
            var cnt = await db.ExecuteScalarAsync<int>(sql, new { codSistema, idSistema });
            return cnt > 0;
        }

        public async Task<bool> ExisteSistemaNombre(string sistema, int? idSistema = null)
        {
            using var db = new SqlConnection(connectionString);
            var sql = idSistema == null
                ? "SELECT COUNT(1) FROM ftc_sistema WHERE sistema = @sistema"
                : "SELECT COUNT(1) FROM ftc_sistema WHERE sistema = @sistema AND idSistema <> @idSistema";
            var cnt = await db.ExecuteScalarAsync<int>(sql, new { sistema, idSistema });
            return cnt > 0;
        }

        public async Task<int> CrearSistema(Sistema nuevo)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                INSERT INTO ftc_sistema (codSistema, sistema, nriesgo)
                VALUES (@codSistema, @sistema, @nriesgo);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await db.ExecuteScalarAsync<int>(sql, nuevo);
        }

        public async Task<int> ActualizarSistema(Sistema editado)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                UPDATE ftc_sistema
                   SET codSistema = @codSistema,
                       sistema    = @sistema,
                       nriesgo    = @nriesgo
                 WHERE idSistema = @idSistema;";
            return await db.ExecuteAsync(sql, editado);
        }
    }
}
