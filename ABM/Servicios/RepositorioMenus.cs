using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
	public interface IRepositorioMenus
	{
		Task<IEnumerable<MenuViewModel>> ObtenerMenusPorRol(int idRol);
		Task<bool> UsuarioTieneAccesoAControlador(int idUsuario, string controlador);
	}
	public class RepositorioMenus : IRepositorioMenus
	{
		private readonly string connectionString;
		private readonly HttpContext httpContext;

		public RepositorioMenus(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
		{
			connectionString = configuration.GetConnectionString("CadenaSQL");
			httpContext = httpContextAccessor.HttpContext;
		}

		public async Task<IEnumerable<MenuViewModel>> ObtenerMenusPorRol(int idRol)
		{
			using var db = new SqlConnection(connectionString);

			// 1) Traer los menús permitidos por rol
			var sqlMenus = @"
SELECT
    m.ID_Menu,
    m.NOMBRE_MENU,
    m.ICONO,
    m.VISTA,
    m.CONTROLADOR
FROM ftc_MENU AS m
INNER JOIN ftc_PermisosMenu AS pm
    ON pm.COD_Menu    = m.ID_Menu
   AND pm.idRol       = @Rol
   AND pm.PERMITIDO   = 1
WHERE m.Estado = 1
ORDER BY m.ID_Menu;";

			var menus = (await db.QueryAsync<MenuViewModel>(
				sqlMenus,
				new { Rol = idRol }
			)).ToList();

			if (!menus.Any())
				return menus;

			// 2) Traer los submenús de esos menús
			var sqlSubs = @"
SELECT
    s.ID_Submenu,
    s.COD_Menu,
    s.NOMBRE_SUBMENU,
    s.VISTA,
    s.CONTROLADOR
FROM ftc_SUBMENU AS s
WHERE s.COD_Menu IN @MenuIds
ORDER BY s.COD_Menu, s.ID_Submenu;";

			var subs = await db.QueryAsync<SubmenuViewModel>(
				sqlSubs,
				new { MenuIds = menus.Select(m => m.ID_Menu).ToArray() }
			);

			// 3) Agrupar submenús en cada menú
			foreach (var menu in menus)
			{
				menu.Submenus = subs
					.Where(s => s.COD_Menu == menu.ID_Menu)
					.ToList();
			}

			return menus;
		}

		// Misma regla con la que se arma el sidebar: el rol del usuario accede a un controller si tiene
		// permitido un menú cuyo CONTROLADOR coincide, o un menú permitido que tenga un submenú de ese controller.
		public async Task<bool> UsuarioTieneAccesoAControlador(int idUsuario, string controlador)
		{
			using var db = new SqlConnection(connectionString);

			var sql = @"
SELECT COUNT(1)
FROM ftc_usuario u
WHERE u.idUsuario = @IdUsuario
  AND EXISTS (
      SELECT 1
      FROM ftc_PermisosMenu pm
      INNER JOIN ftc_MENU m ON m.ID_Menu = pm.COD_Menu AND m.Estado = 1
      WHERE pm.idRol = u.idRol
        AND pm.PERMITIDO = 1
        AND (
              UPPER(m.CONTROLADOR) = UPPER(@Controlador)
           OR EXISTS (
                  SELECT 1 FROM ftc_SUBMENU s
                  WHERE s.COD_Menu = m.ID_Menu
                    AND UPPER(s.CONTROLADOR) = UPPER(@Controlador)
              )
        )
  );";

			var conteo = await db.ExecuteScalarAsync<int>(sql, new { IdUsuario = idUsuario, Controlador = controlador });
			return conteo > 0;
		}

	}
}
