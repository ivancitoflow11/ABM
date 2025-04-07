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
        // Obtiene todos los usuarios (para listar, por ejemplo)
        Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios();

        // Obtiene los datos del usuario que está autenticado
        Task<Usuario> ObtenerDatosUsuarioPerfilLogeado();

        // Valida las credenciales de login (correo y repeat_password)
        Task<Usuario> ValidarUsuario(string correo, string repeat_password);
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

        public async Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return (await dbdapper.QueryAsync<ListaUsuariosViewModel>(
                    @"SELECT * FROM usuario")).ToList();
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
    }
}
