using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace ABM.Servicios
{
    public interface IRepositorioRoles
    {
        Task<IEnumerable<Rol>> ObtenerRoles();
        Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS();
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
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Rol>(
                    @"SELECT idRol, nombre FROM rol ORDER BY nombre;
                        ");
            }
        }

        public async Task<IEnumerable<RolConPNSViewModel>> ObtenerRolesConPNS()
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
        SELECT r.idRol, r.nombre AS nombreRol,
               pns.pais, pns.negocio, pns.sistema, pns.idNegocio
        FROM rol r
        JOIN detalle_rol dr ON r.idRol = dr.idRol
        JOIN PNS pns ON dr.idPaisNegocioSistema = pns.idPaisNegocioSistema
        ORDER BY r.idRol, pns.pais, pns.idNegocio, pns.sistema";

            var datos = await connection.QueryAsync(query);

            // Agrupamos con LINQ
            var resultado = datos
                .GroupBy(x => new { x.idRol, x.nombreRol })
                .Select(grupoRol => new RolConPNSViewModel
                {
                    idRol = grupoRol.Key.idRol,
                    nombreRol = grupoRol.Key.nombreRol,
                    PaisesNegocios = grupoRol
                        .GroupBy(x => new { x.pais, x.negocio })
                        .Select(grupoPN => new PaisNegocioViewModel
                        {
                            pais = grupoPN.Key.pais,
                            negocio = grupoPN.Key.negocio,
                            sistemas = grupoPN.Select(x => (string)x.sistema).Distinct().ToList()
                        }).ToList()
                });

            return resultado;
        }

    }
}
