using ABM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System;
namespace ABM.Servicios
{
    public interface IRepositorioKPI
    {
        Task<IEnumerable<KpiResultado>> ObtenerKpiDiario(int mes, int año);
        Task<(int mes, int año)> ObtenerUltimoMesAnioKpiFinal();
        Task<IEnumerable<(int mes, int anio)>> ObtenerMesesAniosDisponibles();
    }
    public class RepositorioKPI : IRepositorioKPI
    {
        private readonly string connectionString;
        public RepositorioKPI(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }
        public async Task<IEnumerable<KpiResultado>> ObtenerKpiDiario(int mes, int año)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
                SELECT
                    fecha_carga,
                    mes,
                    año,
                    PAIS,
                    n_finiquitados,
                    n_finiquitados_total,
                    cumplimiento_finiquitados, -- Corregido
                    n_60,
                    n_60_total,
                    cumplimiento_60,
                    n_180,
                    n_180_total,
                    cumplimiento_180,
                    n_generica_sin_responsable, -- Corregido
                    n_generica_sin_responsable_total, -- Corregido
                    cumplimiento_generica_sin_responsable, -- Corregido
                    n_usuarios_no_logeados,
                    n_usuarios_no_logeados_total, -- Corregido
                    cumplimiento_usuarios_no_logeados, -- Corregido
                    usuarios_duplicados,
                    n_usuarios_duplicados,
                    n_usuarios_duplicados_total, -- Corregido
                    cumplimiento_usuarios_duplicados, -- Corregido
                    n_usuarios_pass_no_expira,
                    n_usuarios_pass_no_expira_total, -- Corregido
                    cumplimiento_usuarios_pass_no_expira, -- Corregido
                    n_genericos_pass_no_expira, -- Corregido
                    n_genericos_pass_no_expira_total, -- Corregido
                    cumplimiento_genericos_pass_no_expira -- Corregido
                FROM
                    dbo.ftc_kpi_final
                WHERE
                    mes = @Mes AND año = @Anio;
            ";
            return await connection.QueryAsync<KpiResultado>(sql, new { Mes = mes, Anio = año });
        }

        public async Task<(int mes, int año)> ObtenerUltimoMesAnioKpiFinal()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
                SELECT TOP 1
                    mes,
                    año
                FROM
                    dbo.ftc_kpi_final
                ORDER BY
                    año DESC, mes DESC;
            ";
            var resultado = await connection.QuerySingleOrDefaultAsync<dynamic>(sql);
            if (resultado != null)
            {
                return ((int)resultado.mes, (int)resultado.año);
            }
            return (DateTime.Today.Month, DateTime.Today.Year);
        }

        public async Task<IEnumerable<(int mes, int anio)>> ObtenerMesesAniosDisponibles()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
                SELECT DISTINCT
                    mes,
                    año
                FROM
                    dbo.ftc_kpi_final
                ORDER BY
                    año DESC, mes DESC;
            ";
            return await connection.QueryAsync<(int mes, int anio)>(sql);
        }
    }
}