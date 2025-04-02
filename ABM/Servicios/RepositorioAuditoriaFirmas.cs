using Dapper;
using ABM.Data;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioAuditoriaFirmas
    {
        Task<IEnumerable<AuditoriaFirmas>> ObtenerAuditoriaFirmas(int mes, int anio);
    }

    public class RepositorioAuditoriaFirmas : IRepositorioAuditoriaFirmas
    {
        private readonly string connectionString;

        public RepositorioAuditoriaFirmas(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL") + ";Command Timeout=120";
        }

		public async Task<IEnumerable<AuditoriaFirmas>> ObtenerAuditoriaFirmas(int mes, int anio)
		{
			using (IDbConnection db = new SqlConnection(connectionString))
			{
				string query = @"
            SELECT 
                g.ID_gerencia,
                g.Nom_Gerencia,
                g.sistema,
                CASE 
                    WHEN f.idFirma IS NOT NULL THEN 'Firmado'
                    ELSE 'No Firmado'
                END AS EstadoFirma,
                u.nombre AS ResponsableFirma,  -- Solo el nombre, sin el apellido
                f.fechaFirma  -- Fecha de firma
            FROM [ABM_SOFTWARE].[dbo].[im_gerencia] g
            LEFT JOIN [ABM_SOFTWARE].[dbo].[im_firma] f
                ON g.ID_gerencia = f.codGerencia
                AND MONTH(f.fechaFirma) = @Mes
                AND YEAR(f.fechaFirma) = @Anio
            LEFT JOIN [ABM_SOFTWARE].[dbo].[usuario] u
                ON f.codUsuarioResponsable = u.idUsuario
            ORDER BY g.Nom_Gerencia";

				return await db.QueryAsync<AuditoriaFirmas>(query, new { Mes = mes, Anio = anio });
			}
		}

	}
}
