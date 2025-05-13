using Dapper;
using ABM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Net.Mail;
using System.Net;
using System.Net.Http;

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
        private readonly HttpContext httpContext;
        public RepositorioMatrizDiaria(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
            httpContext = httpContextAccessor.HttpContext;
        }



        public async Task<Cl_matriz_diaria> Obtenercl_matriz_diariaPorIdCarga(int IdCarga)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<Cl_matriz_diaria>(@"SELECT * FROM ftc_matriz_diaria
                where idCarga = @IdCarga", new { IdCarga });

            }
        }


        public async Task Actualizarcl_matriz_diariaPorExcepcion(Cl_matriz_diaria cl_matriz_diaria,
            DateTime FechaHasta, string NombreMotivo, string Observaciones)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string Tabla = "ftc_matriz_diaria";


                cl_matriz_diaria.estado_ex = "CERRADO";
                cl_matriz_diaria.fechaEsperaba_ex = FechaHasta;
                cl_matriz_diaria.fechaActual_ex = DateTime.Now;
                cl_matriz_diaria.motivo_ex = NombreMotivo;
                cl_matriz_diaria.fechaModAdmin_ex = DateTime.Now.ToString("yyyy-MM-dd");
                cl_matriz_diaria.comentario_ex = Observaciones;

                await dbdapper.ExecuteAsync(@"UPDATE " + Tabla + @" SET
                                estado_ex = @estado_ex,
                                fechaEsperaba_ex = @fechaEsperaba_ex,
                                fechaActual_ex = @fechaActual_ex,
                                motivo_ex = @motivo_ex,
                                fechaModAdmin_ex = @fechaModAdmin_ex,
                                comentario_ex = @comentario_ex
                                WHERE idCarga = @idCarga", cl_matriz_diaria);
            }


        }


    }
}
