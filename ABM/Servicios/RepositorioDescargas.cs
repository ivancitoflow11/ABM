using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioDescargas
    {
        Task<IEnumerable<dynamic>> ObtenerDatosFiniquitados();
        Task<IEnumerable<dynamic>> ObtenerDatosActivosFalanet();
        Task<IEnumerable<dynamic>> ObtenerDatosAD();
    }

    public class RepositorioDescargas : IRepositorioDescargas
    {
        private readonly string connectionString;

        public RepositorioDescargas(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<IEnumerable<dynamic>> ObtenerDatosFiniquitados()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Se agrega GETDATE() como Fecha_Descarga
                var query = @"
                SELECT 
                    ftc_finiquitados_general.*, 
                    FORMAT(GETDATE(), 'dd-MM-yyyy HH:mm:ss') AS Fecha_Descarga
                FROM ftc_finiquitados_general
                JOIN (
                    SELECT 
                        MAX(fecfiniquito) AS f, 
                        MAX(fecterminocontrato) AS t, 
                        rutdni AS r
                    FROM ftc_finiquitados_general
                    GROUP BY rutdni
                ) x ON ftc_finiquitados_general.fecfiniquito = x.f 
                   AND ftc_finiquitados_general.rutdni = x.r 
                   AND ftc_finiquitados_general.fecterminocontrato = x.t;
                ";

                return await db.QueryAsync<dynamic>(query);
            }
        }

        public async Task<IEnumerable<dynamic>> ObtenerDatosActivosFalanet()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Se agrega GETDATE() como Fecha_Descarga
                var query = @"
                    SELECT *, 
                           FORMAT(GETDATE(), 'dd-MM-yyyy HH:mm:ss') AS Fecha_Descarga 
                    FROM ftc_activos_falanet;";

                return await db.QueryAsync<dynamic>(query);
            }
        }

        public async Task<IEnumerable<dynamic>> ObtenerDatosAD()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Se agrega GETDATE() como Fecha_Descarga
                var query = @"
                    SELECT *, 
                           FORMAT(GETDATE(), 'dd-MM-yyyy HH:mm:ss') AS Fecha_Descarga 
                    FROM ftc_ad;";

                return await db.QueryAsync<dynamic>(query);
            }
        }
    }
}