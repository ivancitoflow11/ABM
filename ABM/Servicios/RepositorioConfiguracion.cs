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

        // --- Métodos para Negocios ---
        Task<IEnumerable<Negocio>> ObtenerNegocios();
        Task<Negocio?> ObtenerNegocioPorId(int idNegocio);
        Task<bool> ExisteNegocioNombre(string nombre, int? idNegocio = null); // Solo validación por nombre
        Task<int> CrearNegocio(Negocio nuevo);
        Task<int> ActualizarNegocio(Negocio editado);

        // --- Métodos para CrucePaisNegocioSistema (CrucePNS) ---
        Task<int> CrearPaisNegocioSistema(PaisNegocioSistema pns);
        Task CrearPnsjt(Pnsjt pnsjt);
        Task<CrucePNSViewModel?> ObtenerCrucePNSPorId(int idPaisNegocioSistema);
        Task<Pnsjt?> ObtenerPnsjtPorIdPaisNegocioSistema(int idPaisNegocioSistema);
        Task<bool> ActualizarPaisNegocioSistema(PaisNegocioSistema pns);
        Task<bool> ActualizarPnsjt(Pnsjt pnsjt);
        Task<IEnumerable<CrucePNSViewModel>> ObtenerCrucesPNS(); // Para la vista de listado
        Task<bool> ExisteCrucePNS(int idPais, int idNegocio, int idSistema, int? idPaisNegocioSistema = null); // Para validación de unicidad
        Task<PaisNegocioSistema?> ObtenerPaisNegocioSistemaPorId(int idPaisNegocioSistema);
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

        // --- METODOS NEGOCIOS ---
        public async Task<IEnumerable<Negocio>> ObtenerNegocios()
        {
            using var db = new SqlConnection(connectionString);
            // En la tabla ftc_negocio, la columna se llama 'negocio', la mapeamos a 'Nombre' en el modelo
            var sql = @"SELECT idNegocio, negocio AS Nombre FROM ftc_negocio";
            return await db.QueryAsync<Negocio>(sql);
        }

        public async Task<Negocio?> ObtenerNegocioPorId(int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT idNegocio, negocio AS Nombre
                  FROM ftc_negocio
                 WHERE idNegocio = @idNegocio";
            return await db.QueryFirstOrDefaultAsync<Negocio>(sql, new { idNegocio });
        }

        public async Task<bool> ExisteNegocioNombre(string nombre, int? idNegocio = null)
        {
            using var db = new SqlConnection(connectionString);
            // La columna en la base de datos es 'negocio'
            var sql = idNegocio == null
                ? "SELECT COUNT(1) FROM ftc_negocio WHERE negocio = @nombre"
                : "SELECT COUNT(1) FROM ftc_negocio WHERE negocio = @nombre AND idNegocio <> @idNegocio";
            var count = await db.ExecuteScalarAsync<int>(sql, new { nombre, idNegocio });
            return count > 0;
        }

        public async Task<int> CrearNegocio(Negocio nuevo)
        {
            using var db = new SqlConnection(connectionString);
            // El modelo tiene 'Nombre', pero la columna en la BD es 'negocio'
            var sql = @"
                INSERT INTO ftc_negocio (negocio) 
                VALUES (@Nombre); 
                SELECT CAST(SCOPE_IDENTITY() AS int);"; // SCOPE_IDENTITY() para obtener el ID insertado
            return await db.ExecuteScalarAsync<int>(sql, nuevo);
        }

        public async Task<int> ActualizarNegocio(Negocio editado)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                UPDATE ftc_negocio
                   SET negocio = @Nombre 
                 WHERE idNegocio = @IdNegocio;";
            return await db.ExecuteAsync(sql, editado);
        }

        // --- para CrucePaisNegocioSistema ---

        public async Task<int> CrearPaisNegocioSistema(PaisNegocioSistema pns)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                INSERT INTO ftc_pais_negocio_sistema (idSistema, idNegocio, idPais, estado)
                VALUES (@IdSistema, @IdNegocio, @IdPais, @Estado);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await db.ExecuteScalarAsync<int>(sql, pns);
        }

        public async Task CrearPnsjt(Pnsjt pnsjt)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                INSERT INTO ftc_pnsjt (tabla, trans, idPaisNegocioSistema, ip, responsable)
                VALUES (@Tabla, @Trans, @IdPaisNegocioSistema, @Ip, @Responsable);";
            await db.ExecuteAsync(sql, pnsjt);
        }

        public async Task<IEnumerable<CrucePNSViewModel>> ObtenerCrucesPNS()
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
        SELECT
            pns.idPaisNegocioSistema,
            pns.idPais,
            pa.pais AS NombrePais,
            pns.idNegocio,
            n.negocio AS NombreNegocio,
            pns.idSistema,
            s.sistema AS NombreSistema,
            pns.estado,
            jt.Id_pns,
            jt.tabla,
            jt.trans,
            jt.ip,
            jt.responsable
        FROM ftc_pais_negocio_sistema pns
        JOIN ftc_pais pa ON pns.idPais = pa.idPais
        JOIN ftc_negocio n ON pns.idNegocio = n.idNegocio
        JOIN ftc_sistema s ON pns.idSistema = s.idSistema
        INNER JOIN ftc_pnsjt jt ON pns.idPaisNegocioSistema = jt.idPaisNegocioSistema 
        ORDER BY pns.idPaisNegocioSistema DESC;";
            return await db.QueryAsync<CrucePNSViewModel>(sql);
        }

        public async Task<CrucePNSViewModel?> ObtenerCrucePNSPorId(int idPaisNegocioSistema)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                SELECT
                    pns.idPaisNegocioSistema,
                    pns.idPais,
                    pns.idNegocio,
                    pns.idSistema,
                    pns.estado,
                    jt.Id_pns, 
                    jt.tabla,
                    jt.trans,
                    jt.ip,
                    jt.responsable
                FROM ftc_pais_negocio_sistema pns
                LEFT JOIN ftc_pnsjt jt ON pns.idPaisNegocioSistema = jt.idPaisNegocioSistema
                WHERE pns.idPaisNegocioSistema = @idPaisNegocioSistema;";
            return await db.QueryFirstOrDefaultAsync<CrucePNSViewModel>(sql, new { idPaisNegocioSistema });
        }

        public async Task<Pnsjt?> ObtenerPnsjtPorIdPaisNegocioSistema(int idPaisNegocioSistema)
        {
            using var db = new SqlConnection(connectionString);
            var sql = "SELECT * FROM ftc_pnsjt WHERE idPaisNegocioSistema = @idPaisNegocioSistema";
            return await db.QueryFirstOrDefaultAsync<Pnsjt>(sql, new { idPaisNegocioSistema });
        }

        public async Task<bool> ActualizarPaisNegocioSistema(PaisNegocioSistema pns)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
                UPDATE ftc_pais_negocio_sistema
                SET idSistema = @IdSistema,
                    idNegocio = @IdNegocio,
                    idPais = @IdPais
                WHERE idPaisNegocioSistema = @IdPaisNegocioSistema;";
            var affectedRows = await db.ExecuteAsync(sql, pns);
            return affectedRows > 0;
        }

        public async Task<bool> ActualizarPnsjt(Pnsjt pnsjt)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @" 
                UPDATE ftc_pnsjt
                SET tabla = @Tabla,
                    trans = @Trans,
                    ip = @Ip,
                    responsable = @Responsable
                WHERE Id_pns = @Id_pns;";
            var affectedRows = await db.ExecuteAsync(sql, pnsjt);
            return affectedRows > 0;
        }

        public async Task<bool> ExisteCrucePNS(int idPais, int idNegocio, int idSistema, int? idPaisNegocioSistema = null)
        {
            using var db = new SqlConnection(connectionString);
            var sqlBase = "SELECT COUNT(1) FROM ftc_pais_negocio_sistema WHERE idPais = @idPais AND idNegocio = @idNegocio AND idSistema = @idSistema";
            var sql = idPaisNegocioSistema == null
                ? sqlBase
                : $"{sqlBase} AND idPaisNegocioSistema <> @idPaisNegocioSistema";
            var count = await db.ExecuteScalarAsync<int>(sql, new { idPais, idNegocio, idSistema, idPaisNegocioSistema });
            return count > 0;
        }

        public async Task<PaisNegocioSistema?> ObtenerPaisNegocioSistemaPorId(int idPaisNegocioSistema)
        {
            using var db = new SqlConnection(connectionString);
            var sql = "SELECT * FROM ftc_pais_negocio_sistema WHERE idPaisNegocioSistema = @idPaisNegocioSistema";
            return await db.QueryFirstOrDefaultAsync<PaisNegocioSistema>(sql, new { idPaisNegocioSistema });
        }
    }
}
