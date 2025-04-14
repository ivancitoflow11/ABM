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


    }
}
