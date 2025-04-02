using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioMenus
    {
        Task<IEnumerable<Menu>> ObtenerMenusRol(int idUsuario);
        Task<IEnumerable<Menu>> ObtenerVistaPrincipal(int idUsuario);
    }
    public class RepositorioMenus : IRepositorioMenus
    {
        private readonly string connectionString;

        public RepositorioMenus(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<Menu>> ObtenerMenusRol(int idUsuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var modelo = await dbdapper.QueryAsync<Menu>(@"SELECT M.* FROM usuario U
                                                                INNER JOIN im_PermisosMenu PM
                                                                on PM.COD_Rol = U.idRol
                                                                INNER JOIN im_Menu M
                                                                on M.ID_Menu = PM.COD_Menu
                                                                WHERE u.idUsuario = @idUsuario
                                                                ORDER BY M.OrdenMenu ASC", new {idUsuario});
                if(modelo != null && modelo.Any())
                {
                    foreach(var item in modelo)
                    {
                       if(item.DESC_Vista == null) {
                            var modelo2 = await dbdapper.QueryAsync<SubMenu>(@"SELECT SM.* FROM usuario U
                                                                                INNER JOIN im_PermisosSubMenu PSM
                                                                                on PSM.COD_Rol = U.idRol
                                                                                INNER JOIN im_SubMenu SM
                                                                                on SM.ID_Submenu = PSM.COD_SubMenu
                                                                                WHERE u.idUsuario = @idUsuario
                                                                                AND SM.COD_Menu = @ID_Menu
                                                                                ORDER BY SM.OrdenMenu ASC", new { idUsuario, item.ID_Menu });
                            item.ListaSubMenu = modelo2;
                       }
                    }
                }
                return modelo;
            }
        }


        public async Task<IEnumerable<Menu>> ObtenerVistaPrincipal(int idUsuario)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Menu>(@"SELECT MP.* 
                FROM usuario U
                INNER JOIN im_PermisosMenu PM ON PM.COD_Rol = U.idRol
                INNER JOIN im_Menu M ON M.ID_Menu = PM.COD_Menu
                INNER JOIN im_Menu MP ON MP.ID_Menu = PM.COD_VistaPrincipal
                WHERE U.idUsuario = @idUsuario
                ", new { idUsuario });
            }
        }
    }
}
