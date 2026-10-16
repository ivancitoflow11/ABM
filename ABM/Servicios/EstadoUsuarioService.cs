using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace ABM.Servicios
{
    // Dice si una cuenta sigue habilitada. Lo usa la cookie de autenticación para cerrar de inmediato las sesiones
    // abiertas de un usuario que se deshabilitó. La respuesta se guarda unos segundos para no consultar la base en
    // cada petición, y se invalida al cambiar el estado desde la aplicación.
    public interface IEstadoUsuarioService
    {
        Task<bool> EstaHabilitadoAsync(int idUsuario);
        void Invalidar(int idUsuario);
    }

    public class EstadoUsuarioService : IEstadoUsuarioService
    {
        public static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(30);

        private readonly IMemoryCache _cache;
        private readonly ILogger<EstadoUsuarioService> _logger;
        private readonly string _cadenaConexion;

        public EstadoUsuarioService(IMemoryCache cache, IConfiguration configuration, ILogger<EstadoUsuarioService> logger)
        {
            _cache = cache;
            _logger = logger;
            _cadenaConexion = configuration.GetConnectionString("CadenaSQL");
        }

        private static string Clave(int idUsuario) => "usuario-habilitado:" + idUsuario;

        public async Task<bool> EstaHabilitadoAsync(int idUsuario)
        {
            var clave = Clave(idUsuario);
            if (_cache.TryGetValue(clave, out bool habilitado))
            {
                return habilitado;
            }

            try
            {
                habilitado = await ConsultarAsync(idUsuario);
            }
            catch (Exception ex)
            {
                // Si la base no responde no se cierran las sesiones de todos: el login y el OTC ya exigen la cuenta
                // habilitada, y sin base la aplicación tampoco puede servir las pantallas.
                _logger.LogWarning(ex, "No se pudo consultar el estado del usuario {IdUsuario}", idUsuario);
                return true;
            }

            _cache.Set(clave, habilitado, Vigencia);
            return habilitado;
        }

        public void Invalidar(int idUsuario) => _cache.Remove(Clave(idUsuario));

        protected virtual async Task<bool> ConsultarAsync(int idUsuario)
        {
            using (IDbConnection db = new SqlConnection(_cadenaConexion))
            {
                return await db.ExecuteScalarAsync<int>(RepositorioUsuarios.ConsultaUsuarioHabilitado, new { idUsuario }) > 0;
            }
        }
    }
}
