using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ABM.Servicios
{
    // 👇 INTERFAZ
    public interface IRepositorioBusquedaMasiva
    {
        Task<int> CrearBusqueda(string usuarioSolicita, string nombreArchivo, int totalRegistros);
        Task ActualizarEstadoBusqueda(int idBusqueda, string estado);
        Task GuardarDetalle(int idBusqueda, DetalleResultadoBusqueda detalle);
        Task<List<DetalleResultadoBusqueda>> ObtenerDetallesBusqueda(int idBusqueda);
    }

    // 👇 MODELO
    public class DetalleResultadoBusqueda
    {
        public string InputBusqueda { get; set; }
        public string RutEncontrado { get; set; }
        public string NombreEncontrado { get; set; }
        public string CorreoEncontrado { get; set; }
        public string EstadoAD { get; set; }
        public string UltimoLoginAD { get; set; }
        public bool EsFiniquitado { get; set; }
        public DateTime? FechaFiniquito { get; set; }
        public bool SprActivo { get; set; }
        public bool EmpCentralActivo { get; set; }
        public string JsonSistemas { get; set; }
        public string JsonNegocios { get; set; }
        public bool TieneProblemas { get; set; }
    }

    // 👇 IMPLEMENTACIÓN DEL REPOSITORIO
    public class RepositorioBusquedaMasiva : IRepositorioBusquedaMasiva
    {
        private readonly string _connectionString;

        public RepositorioBusquedaMasiva(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        public async Task<int> CrearBusqueda(string usuarioSolicita, string nombreArchivo, int totalRegistros)
        {
            using var db = new SqlConnection(_connectionString);

            var sql = @"
                INSERT INTO BusquedaMasiva_Log (UsuarioSolicita, NombreArchivo, TotalRegistros, Estado)
                VALUES (@usuarioSolicita, @nombreArchivo, @totalRegistros, 'En Proceso');
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await db.ExecuteScalarAsync<int>(sql, new { usuarioSolicita, nombreArchivo, totalRegistros });
        }

        public async Task ActualizarEstadoBusqueda(int idBusqueda, string estado)
        {
            using var db = new SqlConnection(_connectionString);

            var sql = "UPDATE BusquedaMasiva_Log SET Estado = @estado WHERE IdBusqueda = @idBusqueda";

            await db.ExecuteAsync(sql, new { idBusqueda, estado });
        }

        public async Task GuardarDetalle(int idBusqueda, DetalleResultadoBusqueda detalle)
        {
            using var db = new SqlConnection(_connectionString);

            var sql = @"
                INSERT INTO BusquedaMasiva_Detalle 
                (IdBusqueda, InputBusqueda, RutEncontrado, NombreEncontrado, CorreoEncontrado, 
                 EstadoAD, UltimoLoginAD, EsFiniquitado, FechaFiniquito, SprActivo, EmpCentralActivo, 
                 JsonSistemas, JsonNegocios, TieneProblemas)
                VALUES 
                (@IdBusqueda, @InputBusqueda, @RutEncontrado, @NombreEncontrado, @CorreoEncontrado, 
                 @EstadoAD, @UltimoLoginAD, @EsFiniquitado, @FechaFiniquito, @SprActivo, @EmpCentralActivo, 
                 @JsonSistemas, @JsonNegocios, @TieneProblemas)";

            await db.ExecuteAsync(sql, new
            {
                IdBusqueda = idBusqueda,
                detalle.InputBusqueda,
                detalle.RutEncontrado,
                detalle.NombreEncontrado,
                detalle.CorreoEncontrado,
                detalle.EstadoAD,
                detalle.UltimoLoginAD,
                detalle.EsFiniquitado,
                detalle.FechaFiniquito,
                detalle.SprActivo,
                detalle.EmpCentralActivo,
                detalle.JsonSistemas,
                detalle.JsonNegocios,
                detalle.TieneProblemas
            });
        }

        public async Task<List<DetalleResultadoBusqueda>> ObtenerDetallesBusqueda(int idBusqueda)
        {
            using var db = new SqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM BusquedaMasiva_Detalle 
                WHERE IdBusqueda = @idBusqueda 
                ORDER BY IdDetalle";

            var resultados = await db.QueryAsync<DetalleResultadoBusqueda>(sql, new { idBusqueda });
            return resultados.AsList();
        }
    }
}