using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient; // O System.Data.SqlClient
using System.Data;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;

namespace ABM.Servicios
{
    public interface IRepositorioMatrizDiaria
    {
        Task Actualizarcl_matriz_diariaPorExcepcion(Cl_matriz_diaria cl_matriz_diaria, DateTime FechaHasta, string NombreMotivo, string Observaciones);
        Task<Cl_matriz_diaria> Obtenercl_matriz_diariaPorIdCarga(int IdCarga);
    }

    public class RepositorioMatrizDiaria : IRepositorioMatrizDiaria
    {
        private readonly string connectionString;

        public RepositorioMatrizDiaria(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<Cl_matriz_diaria> Obtenercl_matriz_diariaPorIdCarga(int IdCarga)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<Cl_matriz_diaria>(@"SELECT * FROM ftc_matriz_diaria
                                                                            WHERE idCarga = @IdCarga", new { IdCarga });
            }
        }

        public async Task Actualizarcl_matriz_diariaPorExcepcion(Cl_matriz_diaria cl_matriz_diaria_param,
            DateTime FechaHastaParam, string NombreMotivo, string Observaciones)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                var parametros = new DynamicParameters();

                parametros.Add("@idCarga", cl_matriz_diaria_param.idCarga);
                parametros.Add("@estado_ex", "CERRADO");
                parametros.Add("@fechaEsperaba_ex", FechaHastaParam.Date, DbType.Date);
                parametros.Add("@fechaActual_ex", DateTime.Now.Date, DbType.Date);
                parametros.Add("@motivo_ex", NombreMotivo);
                parametros.Add("@fechaModAdmin_ex", DateTime.Now.Date, DbType.Date);
                parametros.Add("@comentario_ex", Observaciones);

                Console.WriteLine("Parámetros para Actualizarcl_matriz_diariaPorExcepcion:");
                foreach (var name in parametros.ParameterNames)
                {
                    Console.WriteLine($" - {name}: {parametros.Get<object>(name)}");
                }

                await dbdapper.ExecuteAsync(@"UPDATE ftc_matriz_diaria SET
                                                estado_ex = @estado_ex,
                                                fechaEsperaba_ex = @fechaEsperaba_ex,
                                                fechaActual_ex = @fechaActual_ex,
                                                motivo_ex = @motivo_ex,
                                                fechaModAdmin_ex = @fechaModAdmin_ex,
                                                comentario_ex = @comentario_ex
                                              WHERE idCarga = @idCarga", parametros);
            }
        }
    }
}
