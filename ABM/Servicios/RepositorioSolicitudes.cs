using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioSolicitudes
    {
        Task<bool> CrearSolicitud(int idUsuarioCreador, string asunto, string mensaje);
        Task<List<Solicitud>> ObtenerSolicitudesPorUsuario(int idUsuarioCreador);
        Task<List<Solicitud>> ObtenerTodasLasSolicitudes();
        Task<bool> CambiarEstado(int idSolicitud, string nuevoEstado, int idUsuarioQueActualiza);

        // Reutiliza el sistema de permisos de menú (ftc_MENU / ftc_PermisosMenu) para
        // decidir qué roles pueden administrar solicitudes, igual que el resto de ABM.
        Task<bool> TienePermisoAdministrarSolicitudes(int idRol);
    }

    public class RepositorioSolicitudes : IRepositorioSolicitudes
    {
        private readonly string connectionString;

        public RepositorioSolicitudes(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<bool> CrearSolicitud(int idUsuarioCreador, string asunto, string mensaje)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var query = @"
                INSERT INTO ftc_solicitudes (idUsuarioCreador, asunto, mensaje, estado, FechaCreacion)
                VALUES (@idUsuarioCreador, @asunto, @mensaje, 'Pendiente', GETDATE());";

            var filasAfectadas = await db.ExecuteAsync(query, new { idUsuarioCreador, asunto, mensaje });
            return filasAfectadas > 0;
        }

        public async Task<List<Solicitud>> ObtenerSolicitudesPorUsuario(int idUsuarioCreador)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var query = @"
                SELECT
                    s.idSolicitud,
                    s.idUsuarioCreador,
                    s.asunto,
                    s.mensaje,
                    s.estado,
                    s.FechaCreacion,
                    s.FechaCambioEstado,
                    s.idUsuarioActualizoEstado,
                    ua.nombre AS NombreActualizo
                FROM ftc_solicitudes s
                LEFT JOIN ftc_usuario ua ON ua.idUsuario = s.idUsuarioActualizoEstado
                WHERE s.idUsuarioCreador = @idUsuarioCreador
                ORDER BY s.FechaCreacion DESC;";

            var resultado = await db.QueryAsync<Solicitud>(query, new { idUsuarioCreador });
            return resultado.ToList();
        }

        public async Task<List<Solicitud>> ObtenerTodasLasSolicitudes()
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var query = @"
                SELECT
                    s.idSolicitud,
                    s.idUsuarioCreador,
                    s.asunto,
                    s.mensaje,
                    s.estado,
                    s.FechaCreacion,
                    s.FechaCambioEstado,
                    s.idUsuarioActualizoEstado,
                    (uc.nombre + ' ' + uc.apellidos) AS NombreCreador,
                    uc.correo AS CorreoCreador,
                    ua.nombre AS NombreActualizo
                FROM ftc_solicitudes s
                INNER JOIN ftc_usuario uc ON uc.idUsuario = s.idUsuarioCreador
                LEFT JOIN ftc_usuario ua ON ua.idUsuario = s.idUsuarioActualizoEstado
                ORDER BY s.FechaCreacion DESC;";

            var resultado = await db.QueryAsync<Solicitud>(query);
            return resultado.ToList();
        }

        public async Task<bool> CambiarEstado(int idSolicitud, string nuevoEstado, int idUsuarioQueActualiza)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var query = @"
                UPDATE ftc_solicitudes
                SET estado = @nuevoEstado,
                    FechaCambioEstado = GETDATE(),
                    idUsuarioActualizoEstado = @idUsuarioQueActualiza
                WHERE idSolicitud = @idSolicitud;";

            var filasAfectadas = await db.ExecuteAsync(query, new { idSolicitud, nuevoEstado, idUsuarioQueActualiza });
            return filasAfectadas > 0;
        }

        public async Task<bool> TienePermisoAdministrarSolicitudes(int idRol)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            var query = @"
                SELECT COUNT(1)
                FROM ftc_PermisosMenu pm
                INNER JOIN ftc_MENU m ON m.ID_Menu = pm.COD_Menu
                WHERE pm.idRol = @idRol
                  AND pm.PERMITIDO = 1
                  AND m.CONTROLADOR = 'Solicitudes'
                  AND m.VISTA = 'Admin';";

            var conteo = await db.ExecuteScalarAsync<int>(query, new { idRol });
            return conteo > 0;
        }
    }
}
