using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
        Task<bool> DeshabilitarCrucePNS(int idPaisNegocioSistema);

        // ENVIO CORREOOOOOOOOOOOOOS
        Task<int?> ObtenerIdCorreoListaPorPNSAsync(int idPaisNegocioSistema);
        Task<bool> ActualizarEnvioCorreoListaAsync(EnvioCorreoLista envioLista);
        Task<int?> ObtenerIdPaisNegocioSistemaActivoAsync(int idPais, int idNegocio, int idSistema);
        Task<bool> CrearEnvioCorreoDetalleAsync(EnvioCorreoDetalle correoDetalle); // Nueva firma (devuelve bool)
        Task<IEnumerable<EnvioCorreoDetalleViewModel>> ObtenerEnvioCorreoDetallesVMAsync();
        Task<IEnumerable<Negocio>> ObtenerNegociosPorPaisAsync(int idPais);
        Task<IEnumerable<Sistema>> ObtenerSistemasPorPaisYNegocioAsync(int idPais, int idNegocio);
        Task<EnvioCorreoDetalleViewModel?> ObtenerEnvioCorreoDetalleVMPorIdAsync(int idDetalle);
        Task<bool> ActualizarEnvioCorreoDetalleAsync(EnvioCorreoDetalle correoDetalle);
        Task<bool> EliminarEnvioCorreoDetalleAsync(int idDetalle);
        Task<int> CrearEnvioCorreoListaAsync(EnvioCorreoLista envioLista);

        // --- NUEVOS MÉTODOS PARA GERENCIAS ---
        Task<IEnumerable<Gerencia>> ObtenerGerencias();
        Task<Gerencia> ObtenerGerenciaPorId(int id);
        Task CrearGerencia(Gerencia modelo);
        Task ActualizarGerencia(Gerencia modelo);
        Task<bool> EliminarGerencia(int id);
        Task<bool> ExisteGerenciaNombre(string nombre, int? idExcluir = null);
    }

    public class RepositorioConfiguracion : IRepositorioConfiguracion
    {
        private readonly string connectionString;

        public RepositorioConfiguracion(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<Gerencia>> ObtenerGerencias()
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var sql = "SELECT ID_gerencia as IdGerencia, Nom_Gerencia, sistema FROM dbo.ftc_gerencia ORDER BY Nom_Gerencia;";
            return await db.QueryAsync<Gerencia>(sql);
        }

        public async Task<Gerencia> ObtenerGerenciaPorId(int id)
        {
            using IDbConnection db = new SqlConnection(connectionString);
            var sql = "SELECT ID_gerencia as IdGerencia, Nom_Gerencia, sistema FROM dbo.ftc_gerencia WHERE ID_gerencia = @id;";
            return await db.QuerySingleOrDefaultAsync<Gerencia>(sql, new { id });
        }

        public async Task CrearGerencia(Gerencia modelo)
        {
            using IDbConnection db = new SqlConnection(connectionString);
            var sql = @"INSERT INTO dbo.ftc_gerencia (Nom_Gerencia, sistema) 
                        VALUES (@Nom_Gerencia, @sistema);";
            await db.ExecuteAsync(sql, modelo);
        }

        public async Task ActualizarGerencia(Gerencia modelo)
        {
            using IDbConnection db = new SqlConnection(connectionString);
            var sql = @"UPDATE dbo.ftc_gerencia 
                        SET Nom_Gerencia = @Nom_Gerencia, sistema = @sistema 
                        WHERE ID_gerencia = @IdGerencia;";
            await db.ExecuteAsync(sql, modelo);
        }

        public async Task<bool> EliminarGerencia(int id)
        {
            using IDbConnection db = new SqlConnection(connectionString);
            var sql = "DELETE FROM dbo.ftc_gerencia WHERE ID_gerencia = @id;";
            var affectedRows = await db.ExecuteAsync(sql, new { id });
            return affectedRows > 0; // Devuelve true si se eliminó al menos una fila
        }

        public async Task<bool> ExisteGerenciaNombre(string nombre, int? idExcluir = null)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var sql = @"SELECT CAST(CASE WHEN EXISTS (
                            SELECT 1 
                            FROM dbo.ftc_gerencia 
                            WHERE Nom_Gerencia = @nombre AND (@idExcluir IS NULL OR ID_gerencia <> @idExcluir)
                        ) THEN 1 ELSE 0 END AS BIT)";

            return await db.QuerySingleAsync<bool>(sql, new { nombre, idExcluir });
        }


        // ENVIO CORREOS
        public async Task<int?> ObtenerIdCorreoListaPorPNSAsync(int idPaisNegocioSistema)
        {
            using var db = new SqlConnection(connectionString);

            var sql = @"
            SELECT TOP 1 idCorreos
            FROM ftc_envio_correo_detalle
            WHERE idpaisnegociosistema = @idPaisNegocioSistema;";
            return await db.QuerySingleOrDefaultAsync<int?>(sql, new { idPaisNegocioSistema });
        }

        public async Task<bool> ActualizarEnvioCorreoListaAsync(EnvioCorreoLista envioLista)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
            UPDATE ftc_Envio_Correo_lista
            SET Envio_Diario = @Envio_Diario,
                Envio_Semanal = @Envio_Semanal,
                Envio_Gerente = @Envio_Gerente,
                Envio_Mensual = @Envio_Mensual,
                Envio_Quincenal = @Envio_Quincenal,
                Envio_Jefe = @Envio_Jefe,
                Fecha_Ultima_Carga = @Fecha_Ultima_Carga
            WHERE idCorreos = @IdCorreos;";
            var affectedRows = await db.ExecuteAsync(sql, envioLista);
            return affectedRows > 0;
        }
        public async Task<int> CrearEnvioCorreoListaAsync(EnvioCorreoLista envioLista)
        {
            using var db = new SqlConnection(connectionString);

            // Se quita IdDetalleCorreo del INSERT, ya que ftc_envio_correo_detalle.idCorreos
            // ahora coincidirá con ftc_Envio_Correo_lista.idCorreos (esta última es IDENTITY)
            var sql = @"
    INSERT INTO ftc_Envio_Correo_lista 
        (NombreLista, Envio_Diario, Envio_Semanal, Envio_Gerente, Envio_Mensual, Envio_Quincenal, Envio_Jefe, Tipo_Carga, Fecha_Ultima_Carga)
    VALUES 
        (@NombreLista, @Envio_Diario, @Envio_Semanal, @Envio_Gerente, @Envio_Mensual, @Envio_Quincenal, @Envio_Jefe, @Tipo_Carga, @Fecha_Ultima_Carga);
    SELECT CAST(SCOPE_IDENTITY() AS int);"; 

            var newId = await db.ExecuteScalarAsync<int>(sql, envioLista);
            return newId;
        }

        public async Task<bool> EliminarEnvioCorreoDetalleAsync(int idDetalle) 
        {
            using var db = new SqlConnection(connectionString);
            var sql = "DELETE FROM ftc_envio_correo_detalle WHERE idDetalle = @idDetalle;"; 
            var affectedRows = await db.ExecuteAsync(sql, new { idDetalle });
            return affectedRows > 0;
        }

        public async Task<EnvioCorreoDetalleViewModel?> ObtenerEnvioCorreoDetalleVMPorIdAsync(int idDetalle) 
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
            SELECT
                ecd.idDetalle,          
                ecd.idCorreos,
                ecd.idpaisnegociosistema,
                pns.idPais, pns.idNegocio, pns.idSistema,
                ecd.OSI, ecd.Correo_OSI,
                ecd.Responsable, ecd.Correo_Responsable,
                ecd.Gerente, ecd.Correo_Gerente,
                ecd.Jefe, ecd.Correo_Jefe,
                ecd.Otros_Correos
            FROM ftc_envio_correo_detalle ecd
            JOIN ftc_pais_negocio_sistema pns ON ecd.idpaisnegociosistema = pns.idPaisNegocioSistema
            WHERE ecd.idDetalle = @idDetalle;"; 
            return await db.QuerySingleOrDefaultAsync<EnvioCorreoDetalleViewModel>(sql, new { idDetalle });
        }

        public async Task<bool> ActualizarEnvioCorreoDetalleAsync(EnvioCorreoDetalle correoDetalle)
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
            UPDATE ftc_envio_correo_detalle
            SET idpaisnegociosistema = @IdPaisNegocioSistema,
                OSI = @OSI, Correo_OSI = @Correo_OSI,
                Responsable = @Responsable, Correo_Responsable = @Correo_Responsable,
                Gerente = @Gerente, Correo_Gerente = @Correo_Gerente,
                Jefe = @Jefe, Correo_Jefe = @Correo_Jefe,
                Otros_Correos = @Otros_Correos
            WHERE idDetalle = @idDetalle;"; 
            var affectedRows = await db.ExecuteAsync(sql, correoDetalle);
            return affectedRows > 0;
        }

        public async Task<IEnumerable<Negocio>> ObtenerNegociosPorPaisAsync(int idPais)
        {
            using var db = new SqlConnection(connectionString);
            // Obtiene solo los negocios distintos asociados al país y que estén en un cruce PNS activo
            var sql = @"
        SELECT DISTINCT n.idNegocio, n.negocio AS Nombre
        FROM ftc_negocio n
        JOIN ftc_pais_negocio_sistema pns ON n.idNegocio = pns.idNegocio
        WHERE pns.idPais = @idPais AND pns.estado = '1'
        ORDER BY n.negocio;";
            return await db.QueryAsync<Negocio>(sql, new { idPais });
        }

        public async Task<IEnumerable<Sistema>> ObtenerSistemasPorPaisYNegocioAsync(int idPais, int idNegocio)
        {
            using var db = new SqlConnection(connectionString);
            // Obtiene solo los sistemas distintos asociados al país y negocio, y que estén en un cruce PNS activo
            var sql = @"
        SELECT DISTINCT s.idSistema, s.sistema, s.codSistema, s.nriesgo 
        FROM ftc_sistema s
        JOIN ftc_pais_negocio_sistema pns ON s.idSistema = pns.idSistema
        WHERE pns.idPais = @idPais AND pns.idNegocio = @idNegocio AND pns.estado = '1'
        ORDER BY s.sistema;";
            return await db.QueryAsync<Sistema>(sql, new { idPais, idNegocio });
        }
        public async Task<IEnumerable<EnvioCorreoDetalleViewModel>> ObtenerEnvioCorreoDetallesVMAsync()
		{
			using var db = new SqlConnection(connectionString);
			var sql = @"
        SELECT
            ecd.idDetalle,
            ecd.idCorreos,
            ecd.idpaisnegociosistema,
            p.pais AS NombrePais,      -- Asegúrate que la columna se llame 'pais' en ftc_pais
            n.negocio AS NombreNegocio,  -- Asegúrate que la columna se llame 'negocio' en ftc_negocio
            s.sistema AS NombreSistema, -- Asegúrate que la columna se llame 'sistema' en ftc_sistema
            ecd.OSI,
            ecd.Correo_OSI,
            ecd.Responsable,
            ecd.Correo_Responsable,
            ecd.Gerente,
            ecd.Correo_Gerente,
            ecd.Jefe,
            ecd.Correo_Jefe,
            ecd.Otros_Correos
        FROM ftc_envio_correo_detalle ecd
        JOIN ftc_pais_negocio_sistema pns ON ecd.idpaisnegociosistema = pns.idPaisNegocioSistema
        JOIN ftc_pais p ON pns.idPais = p.idPais
        JOIN ftc_negocio n ON pns.idNegocio = n.idNegocio
        JOIN ftc_sistema s ON pns.idSistema = s.idSistema
        ORDER BY ecd.idCorreos DESC;"; // O el orden que prefieras
			return await db.QueryAsync<EnvioCorreoDetalleViewModel>(sql);
		}
		public async Task<int?> ObtenerIdPaisNegocioSistemaActivoAsync(int idPais, int idNegocio, int idSistema)
		{
			using var db = new SqlConnection(connectionString);
			var sql = @"
        SELECT idPaisNegocioSistema
        FROM ftc_pais_negocio_sistema
        WHERE idPais = @idPais
          AND idNegocio = @idNegocio
          AND idSistema = @idSistema
          AND estado = '1';"; // Solo cruces activos
			return await db.QuerySingleOrDefaultAsync<int?>(sql, new { idPais, idNegocio, idSistema });
		}

        public async Task<bool> CrearEnvioCorreoDetalleAsync(EnvioCorreoDetalle correoDetalle) // Devuelve bool
        {
            using var db = new SqlConnection(connectionString);
            var sql = @"
        INSERT INTO ftc_envio_correo_detalle
            (idCorreos, idpaisnegociosistema, OSI, Correo_OSI, Responsable, Correo_Responsable, Gerente, Correo_Gerente, Jefe, Correo_Jefe, Otros_Correos)
        VALUES
            (@IdCorreos, @IdPaisNegocioSistema, @OSI, @Correo_OSI, @Responsable, @Correo_Responsable, @Gerente, @Correo_Gerente, @Jefe, @Correo_Jefe, @Otros_Correos);";
            // ExecuteAsync devuelve el número de filas afectadas.
            var affectedRows = await db.ExecuteAsync(sql, correoDetalle);
            return affectedRows > 0; // Devuelve true si se insertó la fila.
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
        INSERT INTO ftc_pnsjt (tabla, trans, idPaisNegocioSistema, ip, responsable, infomatrizperfil)
        VALUES (@Tabla, @Trans, @IdPaisNegocioSistema, @Ip, @Responsable, @infomatrizperfil);";
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
        jt.responsable,
        jt.infomatrizperfil
    FROM ftc_pais_negocio_sistema pns
    JOIN ftc_pais pa ON pns.idPais = pa.idPais
    JOIN ftc_negocio n ON pns.idNegocio = n.idNegocio
    JOIN ftc_sistema s ON pns.idSistema = s.idSistema
    INNER JOIN ftc_pnsjt jt ON pns.idPaisNegocioSistema = jt.idPaisNegocioSistema 
    WHERE pns.estado = '1'
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
            jt.responsable,
            jt.infomatrizperfil AS InfoMatrizPerfil
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
            responsable = @Responsable,
            infomatrizperfil = @infomatrizperfil
        WHERE Id_pns = @Id_pns;";
            var affectedRows = await db.ExecuteAsync(sql, pnsjt);
            return affectedRows > 0;
        }

        public async Task<bool> ExisteCrucePNS(int idPais, int idNegocio, int idSistema, int? idPaisNegocioSistema = null)
        {
            using var db = new SqlConnection(connectionString);
            var sqlBase = @"
        SELECT COUNT(1) 
        FROM ftc_pais_negocio_sistema 
        WHERE idPais = @idPais 
          AND idNegocio = @idNegocio 
          AND idSistema = @idSistema 
          AND estado = '1'"; 

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

        public async Task<bool> DeshabilitarCrucePNS(int idPaisNegocioSistema)
        {
            using var db = new SqlConnection(connectionString);
            // Se actualiza el estado a '0' solo si actualmente es '1'.
            var sql = @"
        UPDATE ftc_pais_negocio_sistema
        SET estado = '0'
        WHERE idPaisNegocioSistema = @idPaisNegocioSistema AND estado = '1';";
            var affectedRows = await db.ExecuteAsync(sql, new { idPaisNegocioSistema });
            return affectedRows > 0; 
        }
    }
}
