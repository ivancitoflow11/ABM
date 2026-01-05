using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{

    public interface IRepositorioResumen
    {
        Task<IEnumerable<ResumenSistema>> ObtenerResumenSistemas(DateTime? fecha = null);
        Task<IEnumerable<DateTime>> ObtenerFechasDisponibles();
    }


    public class RepositorioResumen : IRepositorioResumen
    {
        private readonly string connectionString;

        public RepositorioResumen(IConfiguration configuration)
        {

            connectionString = configuration.GetConnectionString("CadenaSQL");
        }
        public async Task<IEnumerable<DateTime>> ObtenerFechasDisponibles()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Traemos solo las fechas únicas que existen en la tabla
                string query = "SELECT DISTINCT CAST(Fecha_Reporte AS DATE) FROM [ABM_FTC].[dbo].[ftc_resumen_sistemas_app] ORDER BY 1 DESC";
                return await dbdapper.QueryAsync<DateTime>(query);
            }
        }

        // En la Implementación (RepositorioResumen.cs):
        public async Task<IEnumerable<ResumenSistema>> ObtenerResumenSistemas(DateTime? fecha = null)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                // Esta lógica hace magia:
                // Si @Fecha es NULL, usa la subconsulta (MAX).
                // Si @Fecha TIENE valor, usa ese valor directamente.
                string query = @"
            SELECT 
                Sistema,
                Finiquitado,
                Activo,
                No_Encontrado,
                Totales,
                Fecha_Reporte,
                Fecha_Actualizacion
            FROM [ABM_FTC].[dbo].[ftc_resumen_sistemas_app]
            WHERE Fecha_Reporte = CASE 
                                    WHEN @Fecha IS NULL THEN (SELECT MAX(Fecha_Reporte) FROM [ABM_FTC].[dbo].[ftc_resumen_sistemas_app])
                                    ELSE @Fecha 
                                  END
            ORDER BY Sistema ASC";

                // Pasamos el parámetro. Si es null, Dapper pasa DBNull.Value
                return await dbdapper.QueryAsync<ResumenSistema>(query, new { Fecha = fecha });
            }
        }
    }
}