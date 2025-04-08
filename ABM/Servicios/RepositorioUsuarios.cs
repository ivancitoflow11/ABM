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


        Task<Usuario> ValidarUsuario(string correo, string repeat_password);
        Task<Usuario> ObtenerPorId(int idUsuario);
        Task<int> RegistrarUsuario(Usuario usuario);
        Task<bool> ExisteCorreo(string correo);
        Task<bool> ExisteUsuario(string usuario);
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<bool> ActualizarPasswordPrimerInicio(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain);

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
        public async Task<bool> ActualizarPasswordPrimerInicio(int idUsuario, string nuevaPasswordHasheada, string nuevaPasswordPlain)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"
            UPDATE usuario
            SET password = @nuevaPasswordHasheada,
                repeat_password = @nuevaPasswordPlain,
                primerInicio = 0,
                FechaCambioPassword = GETDATE()
            WHERE idUsuario = @idUsuario";

                int rows = await dbdapper.ExecuteAsync(query, new { idUsuario, nuevaPasswordHasheada, nuevaPasswordPlain });
                return rows > 0;
            }
        }


        public async Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return (await dbdapper.QueryAsync<ListaUsuariosViewModel>(
                    @"SELECT * FROM usuario")).ToList();
            }
        }

        public async Task<Usuario> ObtenerPorId(int idUsuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(
                    "SELECT * FROM usuario WHERE idUsuario = @id", new { id = idUsuario });
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
                            @"SELECT * FROM usuario WHERE idUsuario = @id", new { id });
                    }
                }
                return null;
            }
        }

        public async Task<Usuario> ValidarUsuario(string correo, string repeat_password)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = @"SELECT * FROM usuario 
                                 WHERE correo = @correo 
                                 AND repeat_password = @repeat_password";
                return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(query, new { correo, repeat_password });
            }
        }

        private int CalcularMesesDiferencia(DateTime fechaInicio, DateTime fechaFin)
        {
            // Calcula la diferencia en años y meses
            int aniosDiferencia = fechaFin.Year - fechaInicio.Year;
            int mesesDiferencia = (aniosDiferencia * 12) + (fechaFin.Month - fechaInicio.Month);

            // Ajuste opcional si consideras que el "día" debe impactar en la cuenta de meses
            // Por ejemplo, si el día actual es menor que el día de la fechaInicio, 
            // podrías restar 1 al conteo, dependiendo de la lógica del negocio.

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
                INSERT INTO usuario (
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
                    idRol
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
                    @idRol
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

        public async Task<bool> ExisteCorreo(string correo)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM usuario WHERE correo = @correo";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { correo });
                return count > 0;
            }
        }

        public async Task<bool> ExisteUsuario(string usuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM usuario WHERE usuario = @usuario";
                int count = await dbdapper.ExecuteScalarAsync<int>(query, new { usuario });
                return count > 0;
            }
        }

        public async Task<IEnumerable<Rol>> ObtenerRoles()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Rol>(
                    @"SELECT idRol, nombre FROM rol ORDER BY nombre;
                        ");
            }
        }

    }
}
