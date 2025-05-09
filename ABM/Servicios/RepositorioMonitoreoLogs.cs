using ABM.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    public interface IRepositorioMonitoreoLogs
    {
        Task RegistrarLog(MonitoreoLog log);
    }
    public class RepositorioMonitoreoLogs : IRepositorioMonitoreoLogs
    {
        private readonly string connectionString;
        public RepositorioMonitoreoLogs(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL")
                ?? throw new InvalidOperationException("La cadena de conexión 'CadenaSQL' no fue encontrada.");
        }

        public async Task RegistrarLog(MonitoreoLog log)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log), "El objeto log no puede ser nulo.");
            }

            using (IDbConnection db = new SqlConnection(connectionString))
            {
                log.Timestamp ??= DateTime.Now;

                string sql = @"
                    INSERT INTO [dbo].[ftc_monitoreoLogs]
                           ([responsable]
                           ,[tarea]
                           ,[tipo]
                           ,[descripcion]
                           ,[resultado]
                           ,[timestamp])
                     VALUES
                           (@Responsable
                           ,@Tarea
                           ,@Tipo
                           ,@Descripcion
                           ,1
                           ,@Timestamp);";
                try
                {
                    await db.ExecuteAsync(sql, log);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Ocurrió un error al intentar registrar el log en la base de datos.", ex);
                }
            }
        }
    }
}