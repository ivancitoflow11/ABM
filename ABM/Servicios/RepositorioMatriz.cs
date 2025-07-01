using ABM.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ABM.Servicios
{



    public interface IRepositorioMatriz
    {
        Task<IEnumerable<MatrizActivo>> ObtenerListaDataMatrizTodo(int idSistema, string idPais, int idNegocio);
        Task<IEnumerable<MatrizDisponible>> ObtenerMatricesDisponibles(int idPais, int idNegocio);
    }
    public class RepositorioMatriz : IRepositorioMatriz
    {
        private readonly string _connectionString;

        public RepositorioMatriz(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        // MÉTODO PARA EL INDEX (CORREGIDO)
        public async Task<IEnumerable<MatrizDisponible>> ObtenerMatricesDisponibles(int idPais, int idNegocio)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                // CAMBIO: Esta es la consulta final y simplificada que sigue el patrón de tu ReportesController.
                const string query = @"
                    SELECT DISTINCT
                        p.pais,
                        s.sistema,
                        s.idSistema
                    FROM
                        dbo.ftc_pais_negocio_sistema pns
                    JOIN
                        dbo.ftc_pais p ON pns.idPais = p.idPais
                    JOIN
                        dbo.ftc_sistema s ON pns.idSistema = s.idSistema
                    JOIN
                        dbo.ftc_agrupa_activos aa ON pns.idPaisNegocioSistema = aa.idPaisNegocioSistema
                    WHERE
                        pns.idPais = @IdPais
                        AND pns.idNegocio = @IdNegocio
                        AND pns.estado = 1
                        AND aa.perfil IS NOT NULL
                    ORDER BY
                        p.pais, s.sistema;
                ";

                return await db.QueryAsync<MatrizDisponible>(query, new { IdPais = idPais, IdNegocio = idNegocio });
            }
        }

        // MÉTODO PARA LA VISTA DE DETALLE (OPTIMIZADO)
        public async Task<IEnumerable<MatrizActivo>> ObtenerListaDataMatrizTodo(int idSistema, string idPais, int idNegocio)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                // CAMBIO: Consulta actualizada para filtrar también por idNegocio.
                const string query = @"
                    SELECT 
                        aa.*,
                        p.pais,
                        s.sistema,
                        g.Nom_Gerencia,
                        sg.Nom_Subgerencia,
                        n.negocio
                    FROM dbo.ftc_agrupa_activos aa
                    JOIN dbo.ftc_pais_negocio_sistema pns ON aa.idPaisNegocioSistema = pns.idPaisNegocioSistema
                    JOIN dbo.ftc_sistema s ON pns.idSistema = s.idSistema
                    JOIN dbo.ftc_pais p ON pns.idPais = p.idPais
                    JOIN dbo.ftc_negocio n ON pns.idNegocio = n.idNegocio
                    LEFT JOIN dbo.ftc_Subgerencias sg ON aa.Nomccostospr = sg.Nom_Subgerencia
                    LEFT JOIN dbo.ftc_gerencia g ON sg.COD_Gerencia = g.ID_gerencia
                    WHERE s.idSistema = @IdSistema 
                      AND p.pais = @IdPais
                      AND pns.idNegocio = @IdNegocio; -- <-- FILTRO AÑADIDO
                ";

                // CAMBIO: Se pasa el nuevo parámetro idNegocio a Dapper.
                return await db.QueryAsync<MatrizActivo>(query, new { idSistema, idPais, idNegocio });
            }
        }
    }
}