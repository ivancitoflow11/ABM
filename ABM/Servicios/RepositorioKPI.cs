using ABM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
namespace ABM.Servicios
{
    public interface IRepositorioKPI
    {
        Task<IEnumerable<KpiResultado>> ObtenerKpiDiario();
    }
    public class RepositorioKPI : IRepositorioKPI
    {
        private readonly string connectionString;
        public RepositorioKPI(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }
        public async Task<IEnumerable<KpiResultado>> ObtenerKpiDiario()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
            DECLARE @FechaMaxima DATE, @PrimerDiaUltimoMes DATE, @UltimoDiaUltimoMes DATE;
DECLARE @PrimerDiaMesAnterior DATE, @UltimoDiaMesAnterior DATE;

-- Obtener la fecha máxima y calcular los rangos de meses
SELECT @FechaMaxima = MAX(CAST(feccarga AS DATE)) FROM dbo.ftc_gestion_diaria;

-- Calcular primer y último día del último mes
SET @PrimerDiaUltimoMes = DATEFROMPARTS(YEAR(@FechaMaxima), MONTH(@FechaMaxima), 1);
SET @UltimoDiaUltimoMes = EOMONTH(@FechaMaxima);

-- Calcular primer y último día del mes anterior
SET @PrimerDiaMesAnterior = DATEADD(MONTH, -1, @PrimerDiaUltimoMes);
SET @UltimoDiaMesAnterior = EOMONTH(@PrimerDiaMesAnterior);

WITH GestionAgregada AS (
    SELECT
        idpaisnegociosistema,
        -- Sumar todos los datos del último mes
        SUM(CASE 
            WHEN CAST(feccarga AS DATE) BETWEEN @PrimerDiaUltimoMes AND @UltimoDiaUltimoMes 
            THEN total_finiquitados ELSE 0 END) AS finiquitados_mes_actual,
        SUM(CASE 
            WHEN CAST(feccarga AS DATE) BETWEEN @PrimerDiaUltimoMes AND @UltimoDiaUltimoMes 
            THEN cnt_usuarios ELSE 0 END) AS activos_mes_actual,
        -- Sumar todos los datos del mes anterior
        SUM(CASE 
            WHEN CAST(feccarga AS DATE) BETWEEN @PrimerDiaMesAnterior AND @UltimoDiaMesAnterior 
            THEN total_finiquitados ELSE 0 END) AS finiquitados_mes_anterior,
        SUM(CASE 
            WHEN CAST(feccarga AS DATE) BETWEEN @PrimerDiaMesAnterior AND @UltimoDiaMesAnterior 
            THEN cnt_usuarios ELSE 0 END) AS activos_mes_anterior
    FROM 
        dbo.ftc_gestion_diaria WITH (NOLOCK)
    WHERE 
        CAST(feccarga AS DATE) BETWEEN @PrimerDiaMesAnterior AND @UltimoDiaUltimoMes
    GROUP BY 
        idpaisnegociosistema
)
SELECT
    pa.pais,
    ne.negocio,
    SUM(CASE WHEN ga.activos_mes_actual > 0 THEN 1 ELSE 0 END) AS sistemas_mes_actual,
    SUM(CASE WHEN ga.activos_mes_anterior > 0 THEN 1 ELSE 0 END) AS sistemas_mes_anterior,
    SUM(ga.finiquitados_mes_actual) AS finiquitados_mes_actual,
    CAST(100.0 - (SUM(CAST(ga.finiquitados_mes_actual AS FLOAT)) * 100.0 / NULLIF(SUM(CAST(ga.activos_mes_actual AS FLOAT)), 0)) AS NUMERIC(13, 1)) AS p_cumplimiento_mes_actual,
    SUM(ga.finiquitados_mes_anterior) AS finiquitados_mes_anterior,
    CAST(100.0 - (SUM(CAST(ga.finiquitados_mes_anterior AS FLOAT)) * 100.0 / NULLIF(SUM(CAST(ga.activos_mes_anterior AS FLOAT)), 0)) AS NUMERIC(16, 1)) AS p_cumplimiento_mes_anterior,
    SUM(ga.activos_mes_actual) AS total_cuentas,
    -- Agregar columnas de variación
    SUM(ga.finiquitados_mes_actual) - SUM(ga.finiquitados_mes_anterior) AS variacion_finiquitados,
    SUM(ga.activos_mes_actual) - SUM(ga.activos_mes_anterior) AS variacion_activos,
    CAST(
        (CAST(100.0 - (SUM(CAST(ga.finiquitados_mes_actual AS FLOAT)) * 100.0 / NULLIF(SUM(CAST(ga.activos_mes_actual AS FLOAT)), 0)) AS FLOAT) -
         CAST(100.0 - (SUM(CAST(ga.finiquitados_mes_anterior AS FLOAT)) * 100.0 / NULLIF(SUM(CAST(ga.activos_mes_anterior AS FLOAT)), 0)) AS FLOAT))
        AS NUMERIC(13, 1)
    ) AS variacion_p_cumplimiento
FROM
    dbo.ftc_pais_negocio_sistema AS pns WITH (NOLOCK)
    INNER JOIN GestionAgregada AS ga ON pns.idPaisNegocioSistema = ga.idpaisnegociosistema
    JOIN dbo.ftc_pais AS pa WITH (NOLOCK) ON pns.idPais = pa.idPais
    JOIN dbo.ftc_negocio AS ne WITH (NOLOCK) ON pns.idNegocio = ne.idNegocio
GROUP BY
    pa.pais,
    ne.negocio
ORDER BY
    pa.pais,
    ne.negocio;
            ";
            return await connection.QueryAsync<KpiResultado>(sql);
        }
    }
}