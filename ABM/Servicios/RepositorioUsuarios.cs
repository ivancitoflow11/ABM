using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ABM.Servicios
{

    public interface IRepositorioUsuarios
    {
        Task<List<ListaUsuariosViewModel>> ObtenerTodosLosUsuarios();
        Task CrearRol(Rol nuevoRol);
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<IEnumerable<PermisosMenu>> ObtenerPermisosMenuPorRol(int idRol);
        Task<IEnumerable<PermisosSubMenu>> ObtenerPermisosSubMenuPorRol(int idRol);
        Task<IEnumerable<Gerencia>> ObtenerGerencias();
        Task<IEnumerable<Menu>> ObtenerMenu();
        Task<IEnumerable<SubMenu>> ObtenerSubMenu();
        Task GuardarPermisosMenu(int codMenu, int codRol, int codVistaPrincipal);
        Task GuardarPermisosSubMenu(int codSubMenu, int codRol);
        Task<Usuario> ObtenerDatosUsuarioPerfilLogeado();

        Task<IEnumerable<Subgerencia>> ObtenerSubGerencias(int? codGerencia);
    }
    public class RepositorioUsuarios : IRepositorioUsuarios
    {
        private string connectionString;
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

        public async Task<IEnumerable<Menu>> ObtenerMenu()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Menu>(
                    @"SELECT [ID_Menu]
                          ,[DESC_NombreMenu]
                    FROM [dbo].[im_Menu];");
            }
        }

        public async Task<IEnumerable<SubMenu>> ObtenerSubMenu()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<SubMenu>(
                    @"SELECT [ID_Submenu]
                          ,[DESC_NombreMenu]
                      FROM [dbo].[im_Submenu]");
            }
        }

        public async Task<IEnumerable<PermisosMenu>> ObtenerPermisosMenuPorRol(int idRol)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<PermisosMenu>(
                    @"SELECT pm.ID_PermisosMenu, pm.COD_Menu, pm.COD_Rol, pm.COD_VistaPrincipal
              FROM im_PermisosMenu pm
              INNER JOIN im_Menu m ON pm.COD_Menu = m.ID_Menu
              INNER JOIN rol r ON pm.COD_Rol = r.idRol
              WHERE r.idRol = @idRol",
                    new { idRol }
                );
            }
        }

        public async Task<IEnumerable<PermisosSubMenu>> ObtenerPermisosSubMenuPorRol(int idRol)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<PermisosSubMenu>(
                    @"SELECT ps.ID_PermisosSubMenu, ps.COD_SubMenu, ps.COD_Rol
              FROM im_PermisosSubMenu ps
              INNER JOIN im_Submenu sm ON ps.COD_SubMenu = sm.ID_Submenu
              INNER JOIN rol r ON ps.COD_Rol = r.idRol
              WHERE r.idRol = @idRol",
                    new { idRol }
                );
            }
        }

        public async Task<IEnumerable<Rol>> ObtenerRoles()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Rol>(
                    @"SELECT idRol, nombre FROM rol");
            }
        }

        public async Task<IEnumerable<Gerencia>> ObtenerGerencias()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Gerencia>(
                    @"SELECT ID_gerencia, Nom_Gerencia 
                    FROM im_gerencia
                    ORDER BY Nom_Gerencia ASC;
                    ");
            }
        }

        public async Task<IEnumerable<Subgerencia>> ObtenerSubGerencias(int? codGerencia)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT 
                ID_Subgerencia,
                Nom_Subgerencia,
                COD_Gerencia
            FROM 
                [ABM_SOFTWARE].[dbo].[im_Subgerencias]
            WHERE 
                (@COD_Gerencia IS NULL OR COD_Gerencia = @COD_Gerencia);
        ";

                return await dbdapper.QueryAsync<Subgerencia>(query, new { COD_Gerencia = codGerencia });
            }
        }


        public async Task CrearRol(Rol nuevoRol)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var sql = @"INSERT INTO rol (nombre)
                    VALUES (@Nombre);
                    SELECT CAST(SCOPE_IDENTITY() as int)"; // Obtiene el idRol generado

                nuevoRol.idRol = await dbdapper.QuerySingleAsync<int>(sql, new { Nombre = nuevoRol.nombre });
            }
        }

        public async Task GuardarPermisosMenu(int codMenu, int codRol, int codVistaPrincipal)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var sql = @"INSERT INTO im_PermisosMenu (COD_Menu, COD_Rol, COD_VistaPrincipal)
                    VALUES (@COD_Menu, @COD_Rol, @COD_VistaPrincipal)";

                var parameters = new
                {
                    COD_Menu = codMenu,
                    COD_Rol = codRol,
                    COD_VistaPrincipal = codVistaPrincipal
                };

                await dbdapper.ExecuteAsync(sql, parameters);
            }
        }


        public async Task GuardarPermisosSubMenu(int codSubMenu, int codRol)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var sql = @"INSERT INTO im_PermisosSubMenu (COD_SubMenu, COD_Rol)
                    VALUES (@COD_SubMenu, @COD_Rol)";

                var parameters = new
                {
                    COD_SubMenu = codSubMenu,
                    COD_Rol = codRol
                };

                await dbdapper.ExecuteAsync(sql, parameters);
            }
        }
        public async Task<Usuario> ObtenerDatosUsuarioPerfilLogeado()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                if (httpContext.User.Identity.IsAuthenticated)
                {
                    var idClain = httpContext.User.Claims
                    .Where(x => x.Type == ClaimTypes.NameIdentifier)
                    .FirstOrDefault();


                    var Id = Convert.ToInt32(idClain.Value);

                    return await dbdapper.QueryFirstOrDefaultAsync<Usuario>(@"SELECT * FROM usuario
                    WHERE idUsuario = @Id", new { Id });
                }

                return null;

            }
        }




    }
}
