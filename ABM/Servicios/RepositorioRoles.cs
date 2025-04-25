using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    public interface IRepositorioRoles
    {
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS();

        // métodos para el proceso de creación de rol
        Task<int> CrearRol(RolModel rol);

        Task<int> CrearDetalleRol(DetalleRolModel detalle);
        Task<IEnumerable<PaisNegocioSistemaModel>> ObtenerPaisNegocioSistemasActivos();
        Task<bool> ExisteRolConNombre(string nombreRol);

        //Menus
        Task<IEnumerable<MenuModel>> ObtenerMenus();
        Task InsertarPermisosMenu(int idRol, List<int> listaMenusSeleccionados);
		Task<IEnumerable<string>> ObtenerVistasPorMenusAsync(List<int> idsMenus);

    }

    public class RepositorioRoles : IRepositorioRoles
    {
        private readonly string connectionString;
        private readonly HttpContext httpContext;

        public RepositorioRoles(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<IEnumerable<Rol>> ObtenerRoles()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return await db.QueryAsync<Rol>(
                    @"SELECT idRol, nombre FROM ftc_rol ORDER BY nombre;");
            }
        }

        public async Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS()
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
SELECT 
    r.idRol, 
    r.nombre AS nombreRol,
    pns.idPaisNegocioSistema,
    pa.pais, 
    pns.idPais,
    ne.negocio, 
    pns.idNegocio,
    si.sistema,
    si.codSistema,
    pns.idSistema,
    pa.codPais
FROM ftc_rol r
JOIN ftc_detalle_rol dr ON r.idRol = dr.idRol
JOIN ftc_pais_negocio_sistema pns ON dr.idPaisNegocioSistema = pns.idPaisNegocioSistema
JOIN ftc_pais pa ON pa.idPais = pns.idPais
JOIN ftc_negocio ne ON ne.idNegocio = pns.idNegocio
JOIN ftc_sistema si ON si.idSistema = pns.idSistema
ORDER BY r.idRol, pa.pais, pns.idNegocio, si.sistema";

            var datos = await connection.QueryAsync(query);

            var resultado = datos
                .GroupBy(x => new { x.idRol, x.nombreRol })
                .Select(grupoRol => new RolConPNSViewModel
                {
                    idRol = grupoRol.Key.idRol,
                    nombreRol = grupoRol.Key.nombreRol,
                    PaisesNegocios = grupoRol
                        .GroupBy(x => new { x.pais, x.idPais, x.negocio, x.idNegocio })
                        .Select(grupoPN => new PaisNegocioViewModel
                        {
                            pais = grupoPN.Key.pais,
                            idPais = grupoPN.Key.idPais,
                            negocio = grupoPN.Key.negocio,
                            idNegocio = grupoPN.Key.idNegocio,
                            sistemas = grupoPN.Select(x => (string)x.sistema).Distinct().ToList()
                        }).ToList()
                });

            return resultado;
        }


        // Insertar un nuevo rol y retornar el ID generado.
        public async Task<int> CrearRol(RolModel rol)

        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
                INSERT INTO ftc_rol (nombre, idPais, idNegocio, idVistaInicio)
                VALUES (@Nombre, @IdPais, @IdNegocio, @IdVistaInicio);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await db.ExecuteScalarAsync<int>(query, rol);
            }
        }

        // Insertar en la tabla ftc_detalle_rol y retornar el ID generado.
        public async Task<int> CrearDetalleRol(DetalleRolModel detalle)

        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
                INSERT INTO ftc_detalle_rol (idRol, idPaisNegocioSistema)
                VALUES (@IdRol, @IdPaisNegocioSistema);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await db.ExecuteScalarAsync<int>(query, detalle);
            }
        }

        // Obtener las combinaciones activas de país, negocio y sistema (vista PNS)
        public async Task<IEnumerable<PaisNegocioSistemaModel>> ObtenerPaisNegocioSistemasActivos()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
                SELECT 
                    PNS.idPaisNegocioSistema, 
                    p.pais, 
                    PNS.idPais, 
                    n.negocio, 
                    PNS.idNegocio, 
                    s.sistema, 
                    s.codSistema, 
                    PNS.idSistema, 
                    p.codPais
                FROM ftc_pais_negocio_sistema AS PNS
                INNER JOIN ftc_pais p ON p.idPais = PNS.idPais
                INNER JOIN ftc_negocio n ON n.idNegocio = PNS.idNegocio
                INNER JOIN ftc_sistema s ON s.idSistema = PNS.idSistema
                WHERE PNS.estado = 1;";

                return await db.QueryAsync<PaisNegocioSistemaModel>(query);
            }
        }

        // 1) Obtener Menús
        public async Task<IEnumerable<MenuModel>> ObtenerMenus()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
                SELECT 
                    [ID_Menu]       AS IdMenu, 
                    [NOMBRE_MENU]   AS NombreMenu,
                    [ICONO]         AS Icono,
                    [VISTA]         AS Vista,
                    [CONTROLADOR]   AS Controlador
                FROM [ftc_MENU]
                ORDER BY [ID_Menu];
            ";
                return await db.QueryAsync<MenuModel>(sql);
            }
        }

        // 2) Insertar Permisos de Menú en la tabla ftc_PermisosMenu
        public async Task InsertarPermisosMenu(int idRol, List<int> listaMenusSeleccionados)
        {
            // Para PERMITIDO = 1 y FECHA_CREACION = GETDATE() en cada menú marcado
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
                INSERT INTO [ftc_PermisosMenu]
                (idRol, COD_Menu, PERMITIDO, FECHA_CREACION)
                VALUES (@IdRol, @CodMenu, 1, GETDATE());
            ";
                foreach (var menuId in listaMenusSeleccionados)
                {
                    var parametros = new
                    {
                        IdRol = idRol,
                        CodMenu = menuId
                    };
                    await db.ExecuteAsync(sql, parametros);
                }
            }
        }

		public async Task<IEnumerable<string>> ObtenerVistasPorMenusAsync(List<int> idsMenus)
		{
			if (idsMenus == null || !idsMenus.Any())
			{
				return Enumerable.Empty<string>();
			}

			using (IDbConnection db = new SqlConnection(connectionString))
			{
				var sql = @"
            SELECT DISTINCT VISTA
            FROM [ftc_MENU]
            WHERE ID_Menu IN @Ids
              AND VISTA IS NOT NULL
              AND VISTA <> ''
            ORDER BY VISTA;
        ";
				var resultado = await db.QueryAsync<string>(sql, new { Ids = idsMenus });
				return resultado;
			}
		}

        public async Task<bool> ExisteRolConNombre(string nombreRol)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT COUNT(1) 
            FROM ftc_rol 
            WHERE nombre = @Nombre";

                int count = await db.ExecuteScalarAsync<int>(query, new { Nombre = nombreRol });
                return count > 0;
            }
        }

    }
}
