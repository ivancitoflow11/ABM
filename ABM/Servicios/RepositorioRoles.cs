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
        // Métodos existentes
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS();

        // Nuevos métodos para el proceso de creación de rol
        Task<int> CrearRol(RolModel rol);

        Task<int> CrearDetalleRol(DetalleRolModel detalle);
        Task<IEnumerable<PaisNegocioSistemaModel>> ObtenerPaisNegocioSistemasActivos();

        //Menus
        Task<IEnumerable<MenuModel>> ObtenerMenus();
        Task InsertarPermisosMenu(int idRol, List<int> listaMenusSeleccionados);
		Task<IEnumerable<string>> ObtenerVistasPorMenusAsync(List<int> idsMenus);

        Task<IEnumerable<VistaInicioModel>> ObtenerVistasInicio();

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
                    @"SELECT idRol, nombre FROM rol ORDER BY nombre;");
            }
        }

        public async Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS()
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
    SELECT r.idRol, r.nombre AS nombreRol,
           pns.pais, pns.idPais,
           pns.negocio, pns.idNegocio,
           pns.sistema
    FROM rol r
    JOIN detalle_rol dr ON r.idRol = dr.idRol
    JOIN PNS pns ON dr.idPaisNegocioSistema = pns.idPaisNegocioSistema
    ORDER BY r.idRol, pns.pais, pns.idNegocio, pns.sistema";

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

        // Inserta un nuevo rol y retorna el ID generado.
        public async Task<int> CrearRol(RolModel rol)

        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
                INSERT INTO rol (nombre, idPais, idNegocio, idVistaInicio)
                VALUES (@Nombre, @IdPais, @IdNegocio, @IdVistaInicio);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await db.ExecuteScalarAsync<int>(query, rol);
            }
        }

        // Inserta en la tabla detalle_rol y retorna el ID generado.
        public async Task<int> CrearDetalleRol(DetalleRolModel detalle)

        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var query = @"
                INSERT INTO detalle_rol (idRol, idPaisNegocioSistema)
                VALUES (@IdRol, @IdPaisNegocioSistema);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await db.ExecuteScalarAsync<int>(query, detalle);
            }
        }

        // Obtiene las combinaciones activas de país, negocio y sistema (vista PNS)
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
                FROM pais_negocio_sistema AS PNS
                INNER JOIN pais p ON p.idPais = PNS.idPais
                INNER JOIN negocio n ON n.idNegocio = PNS.idNegocio
                INNER JOIN sistema s ON s.idSistema = PNS.idSistema
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
                FROM [Abm_APP].[dbo].[ftc_MENU]
                ORDER BY [ID_Menu];
            ";
                return await db.QueryAsync<MenuModel>(sql);
            }
        }

        // 2) Insertar Permisos de Menú en la tabla ftc_PermisosMenu
        public async Task InsertarPermisosMenu(int idRol, List<int> listaMenusSeleccionados)
        {
            // Suponiendo que PERMITIDO = 1 y FECHA_CREACION = GETDATE() para cada menú marcado
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
                INSERT INTO [Abm_APP].[dbo].[ftc_PermisosMenu]
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
            FROM [Abm_APP].[dbo].[ftc_MENU]
            WHERE ID_Menu IN @Ids
              AND VISTA IS NOT NULL
              AND VISTA <> ''
            ORDER BY VISTA;
        ";
				var resultado = await db.QueryAsync<string>(sql, new { Ids = idsMenus });
				return resultado;
			}
		}

        public async Task<IEnumerable<VistaInicioModel>> ObtenerVistasInicio()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT 
                [idVistaInicio] AS IdVistaInicio,
                [vistaInicio]   AS VistaInicio
            FROM [Abm_APP].[dbo].[vista_inicio]
            ORDER BY vistaInicio;";
                return await db.QueryAsync<VistaInicioModel>(sql);
            }
        }

    }
}
