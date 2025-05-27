using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
    public interface IRepositorioUsuarios
    {

        Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios();

        Task<Usuario> ObtenerDatosUsuarioPerfilLogeado();
        Task<Home> ObtenerDatosUsuarioHome();

        Task<Usuario> ValidarUsuario(string correo, string repeat_password);
        Task<Usuario> ObtenerPorId(int idUsuario);
        Task<int> RegistrarUsuario(Usuario usuario);
        Task<bool> ActualizarUsuario(Usuario usuario);
        Task<bool> ExisteCorreo(string correo);
        Task<bool> ExisteUsuario(string usuario);
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<bool> ActualizarPasswordPrimerInicio(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain);

        Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuariosYRoles();
        Task<bool> ActualizarOTC(int idUsuario, int codigoOTC);
        Task<int?> ObtenerIdRolDeUsuario(int idUsuario);

        // Nuevos métodos para olvido de contraseña:
        Task<Usuario> ObtenerUsuarioPorCorreo(string correo);
        Task<bool> ActualizarTokenRestablecimiento(int idUsuario, string token, DateTime tokenExpiry);
        Task<Usuario> ObtenerUsuarioPorTokenRestablecimiento(string token); // Este método debe verificar también la expiración internamente o devolverla.
        Task<bool> ActualizarPasswordYConsumirToken(int idUsuario, string nuevaPasswordHashed, string nuevaPasswordOriginal); //Similar a primer inicio, podría necesitar la original para historial
        //METODO PARA PASO ACTIVOS

        Task<bool> ExisteCorreoEnPasoActivos(string correo);
        Task<bool> ExisteCorreoEnAD(string correo);

        //AJUSTES PERFIL
        Task<bool> ActualizarDatosPerfil(Usuario usuario);
		Task<bool> ActualizarPasswordUsuarioLogeado(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain);
	}

    public class RepositorioUsuarios : IRepositorioUsuarios
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioUsuarios(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }

		public async Task<bool> ActualizarPasswordUsuarioLogeado(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				// Esta consulta actualiza la contraseña (hasheada y en texto plano para repeat_password),
				// la fecha de cambio de contraseña y establece el estado de la contraseña como activo (asumiendo 1).
				// No modifica 'primerInicio'.
				string query = @"
                UPDATE ftc_usuario
                SET 
                    password = @PasswordHasheada,
                    repeat_password = @PasswordPlain, -- ¡Considera las implicaciones de seguridad!
                    FechaCambioPassword = GETDATE(),
                    estado_password = 1 -- Asumiendo que 1 significa que la contraseña está activa/válida
                    -- Opcional: podrías considerar limpiar ResetPasswordToken y ResetPasswordTokenExpiry aquí también si fuera relevante
                    -- ResetPasswordToken = NULL,
                    -- ResetPasswordTokenExpiry = NULL 
                WHERE idUsuario = @IdUsuario;";

				var parameters = new
				{
					IdUsuario = idUsuario,
					PasswordHasheada = nuevaPasswordHasheada,
					PasswordPlain = nuevaPasswordPlain
				};

				int rowsAffected = await dbdapper.ExecuteAsync(query, parameters);
				return rowsAffected > 0;
			}
		}
		public async Task<bool> ActualizarDatosPerfil(Usuario usuario)
        {

            using (IDbConnection db = new SqlConnection(connectionString))
            {

                string query = @"
                UPDATE ftc_usuario SET
                    Nombre = @Nombre,
                    Apellidos = @Apellidos,
                    Telefono = @Telefono,
                    FotoUrl = @FotoUrl, 
                    FechaNacimiento = @FechaNacimiento,
                    FultimaModificacion = GETDATE() 
                WHERE idUsuario = @idUsuario;";

                var parameters = new
                {
                    usuario.nombre,
                    usuario.apellidos,
                    usuario.telefono,
                    usuario.FotoUrl, 
                    usuario.FechaNacimiento,
                    usuario.idUsuario
                };

                int rowsAffected = await db.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
        }
        public async Task<bool> ActualizarPasswordPrimerInicio(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
            UPDATE ftc_usuario
            SET password = @nuevaPasswordHasheada,
                repeat_password = @nuevaPasswordPlain,
                primerInicio = 0,
                FechaCambioPassword = GETDATE()
            WHERE idUsuario = @idUsuario";

                int rows = await dbdapper.ExecuteAsync(query, new { idUsuario, nuevaPasswordHasheada, nuevaPasswordPlain });
                return rows > 0;
            }
        }
        public async Task<bool> ExisteCorreoEnPasoActivos(string correo)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM ftc_paso_activos WHERE mailusuario = @correo";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { correo });
                return count > 0;
            }
        }

        public async Task<bool> ExisteCorreoEnAD(string correo)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT COUNT(1) 
                FROM dbo.ftc_ad
                WHERE mail = @correo 
                AND userAccountControl IN (512, 544, 66048, 66080)";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { correo });
                return count > 0;
            }
        }
        public async Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuariosYRoles()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT 
                u.idUsuario,
                u.nombre, 
                u.apellidos, 
                u.usuario, 
                u.correo, 
                u.rut,
                u.telefono,
                r.nombre AS RolNombre
            FROM ftc_usuario u
            INNER JOIN ftc_rol r ON u.idRol = r.idRol
            ORDER BY u.nombre ASC, u.apellidos ASC;
            ";

                var resultado = await dbdapper.QueryAsync<ListaUsuariosViewModel>(query);
                return resultado.ToList();
            }
        }



        public async Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return (await dbdapper.QueryAsync<ListaUsuariosViewModel>(
                    @"SELECT * FROM ftc_usuario")).ToList();
            }
        }

        public async Task<Usuario> ObtenerPorId(int idUsuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(
                    "SELECT * FROM ftc_usuario WHERE idUsuario = @id", new { id = idUsuario });
            }
        }


        public async Task<Usuario> ObtenerDatosUsuarioPerfilLogeado()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                if (httpContext.User.Identity.IsAuthenticated)
                {
                    var idClaim = httpContext.User.Claims
                        .Where(x => x.Type == ClaimTypes.NameIdentifier)
                        .FirstOrDefault();

                    if (idClaim != null)
                    {
                        int id = Convert.ToInt32(idClaim.Value);
                        return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(
                            @"SELECT * FROM ftc_usuario WHERE idUsuario = @id", new { id });
                    }
                }
                return null;
            }
        }

        public async Task<Home> ObtenerDatosUsuarioHome()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                if (httpContext.User.Identity.IsAuthenticated)
                {
                    var idClaim = httpContext.User.Claims
                        .Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                        .FirstOrDefault();

                    if (idClaim != null)
                    {
                        int id = Convert.ToInt32(idClaim.Value);
                        string query = @"
                        SELECT
                            u.idUsuario,
                            u.nombre,
                            u.apellidos,
                            u.correo,
                            u.inicioOtc,
                            u.idRol,
                            u.rut,         
                            u.telefono,   
                            u.usuario, 
                            r.nombre AS RolNombre
                        FROM ftc_usuario u
                        LEFT JOIN ftc_rol r ON u.idRol = r.idRol
                        WHERE u.idUsuario = @id";
                        return await dbdapper.QueryFirstOrDefaultAsync<Home>(query, new { id });
                    }
                }
                return null;
            }
        }

        public async Task<Usuario> ValidarUsuario(string correo, string repeat_password)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"SELECT * FROM ftc_usuario 
                                 WHERE correo = @correo 
                                 AND repeat_password = @repeat_password";
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(query, new { correo, repeat_password });
            }
        }

        private int CalcularMesesDiferencia(DateTime fechaInicio, DateTime fechaFin)
        {
            // Cálculo de la diferencia en años y meses
            int aniosDiferencia = fechaFin.Year - fechaInicio.Year;
            int mesesDiferencia = (aniosDiferencia * 12) + (fechaFin.Month - fechaInicio.Month);

            if (fechaFin.Day < fechaInicio.Day)
            {
                mesesDiferencia--;
            }

            return mesesDiferencia;
        }

        public async Task<int> RegistrarUsuario(Usuario usuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                try
                {
                    string query = @"
                INSERT INTO ftc_usuario (
                    nombre, 
                    apellidos, 
                    rut, 
                    telefono, 
                    correo, 
                    usuario, 
                    password, 
                    repeat_password, 
                    Fcreacion, 
                    otc, 
                    inicioOtc, 
                    MesesExpiracionClave,
                    estado,
                    estado_password,
                    idRol,
                    primerInicio
                )
                VALUES (
                    @nombre, 
                    @apellidos, 
                    @rut, 
                    @telefono, 
                    @correo, 
                    @usuario, 
                    @password, 
                    @repeat_password, 
                    @Fcreacion, 
                    @otc, 
                    @inicioOtc, 
                    @MesesExpiracionClave,
                    @estado,
                    '1',
                    @idRol,
                    '1'
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";
                    return await dbdapper.ExecuteScalarAsync<int>(query, usuario);
                }
                catch (Exception ex)
                {
                    // Aquí puedes registrar el error o mostrarlo para depuración
                    throw new Exception("Error al registrar el usuario: " + ex.Message, ex);
                }
            }
        }

        public async Task<bool> ActualizarUsuario(Usuario usuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
            UPDATE ftc_usuario
            SET nombre = @nombre,
                apellidos = @apellidos,
                rut = @rut,
                telefono = @telefono,
                correo = @correo,
                usuario = @usuario,
                idRol = @idRol,
                FultimaModificacion = GETDATE()
            WHERE idUsuario = @idUsuario";

                int filas = await dbdapper.ExecuteAsync(query, usuario);
                return filas > 0;
            }
        }

        public async Task<bool> ExisteCorreo(string correo)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM ftc_usuario WHERE correo = @correo";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { correo });
                return count > 0;
            }
        }

        public async Task<bool> ExisteUsuario(string usuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM ftc_usuario WHERE usuario = @usuario";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { usuario });
                return count > 0;
            }
        }

        public async Task<IEnumerable<Rol>> ObtenerRoles()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Rol>(
                    @"SELECT idRol, nombre FROM ftc_rol ORDER BY nombre;
                        ");
            }
        }

        public async Task<bool> ActualizarOTC(int idUsuario, int codigoOTC)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "UPDATE ftc_usuario SET otc = @codigoOTC, inicioOtc = GETDATE() WHERE idUsuario = @idUsuario";
                int filas = await dbdapper.ExecuteAsync(query, new { idUsuario, codigoOTC });
                return filas > 0;
            }
        }

        public async Task<int?> ObtenerIdRolDeUsuario(int idUsuario)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return await db.QuerySingleOrDefaultAsync<int?>("SELECT idRol FROM ftc_usuario WHERE idUsuario = @idUsuario", new { idUsuario });
            }
        }

        // --- Nuevos métodos para olvido de contraseña ---

        public async Task<Usuario> ObtenerUsuarioPorCorreo(string correo)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Asegúrate de que el usuario también esté activo (ej: estado = 1)
                // Se seleccionan todas las columnas para que el objeto Usuario se hidrate completamente.
                string query = @"SELECT * FROM ftc_usuario 
                         WHERE correo = @Correo AND estado = 1"; // Asumiendo estado = 1 para activo
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(query, new { Correo = correo });
            }
        }

        public async Task<bool> ActualizarTokenRestablecimiento(int idUsuario, string token, DateTime tokenExpiry)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
        UPDATE ftc_usuario
        SET ResetPasswordToken = @Token,
            ResetPasswordTokenExpiry = @TokenExpiry
        WHERE idUsuario = @IdUsuario";

                int rows = await dbdapper.ExecuteAsync(query, new { IdUsuario = idUsuario, Token = token, TokenExpiry = tokenExpiry });
                return rows > 0;
            }
        }

        public async Task<Usuario> ObtenerUsuarioPorTokenRestablecimiento(string token)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // El controller debería verificar la expiración (ResetPasswordTokenExpiry) después de obtener el usuario.
                // Se seleccionan todas las columnas para que el objeto Usuario se hidrate completamente,
                // incluyendo ResetPasswordTokenExpiry.
                string query = @"SELECT * FROM ftc_usuario 
                         WHERE ResetPasswordToken = @Token AND estado = 1"; // Asumiendo estado = 1 para activo
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(query, new { Token = token });
            }
        }

        public async Task<bool> ActualizarPasswordYConsumirToken(int idUsuario, string nuevaPasswordHashed, string nuevaPasswordOriginal)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // ADVERTENCIA: Guardar 'nuevaPasswordOriginal' en 'repeat_password' (si es texto plano) es inseguro.
                // 'password' debe ser el hash seguro.
                // 'estado_password' se asume 1 para indicar que la contraseña está activa/cambiada.
                // 'primerInicio' se establece a 0.
                string query = @"
        UPDATE ftc_usuario
        SET password = @PasswordHash,
            repeat_password = @PasswordOriginal, -- ¡REVISAR SEGURIDAD DE ESTE CAMPO!
            FechaCambioPassword = GETDATE(), -- Considera DateTime.UtcNow desde C# para consistencia
            ResetPasswordToken = NULL,
            ResetPasswordTokenExpiry = NULL,
            primerInicio = 0,
            estado_password = 1 -- Asumiendo 1 = contraseña activa/cambiada
        WHERE idUsuario = @IdUsuario";

                int rows = await dbdapper.ExecuteAsync(query, new
                {
                    IdUsuario = idUsuario,
                    PasswordHash = nuevaPasswordHashed,
                    PasswordOriginal = nuevaPasswordOriginal
                });
                return rows > 0;
            }
        }

    }
}
