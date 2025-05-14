using ABM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq; // Necesario para .FirstOrDefault()

namespace ABM.Servicios
{
    public interface IRepositorioListaBlanca
    {
        Task<IEnumerable<ListaBlanca>> ObtenerTodos();
        Task<ListaBlanca> ObtenerPorRutNumDocumento(string rutNumDocumento);
        Task<bool> Crear(ListaBlanca modelo);
        Task<bool> Actualizar(ListaBlanca modelo);
        Task<bool> Eliminar(string rutNumDocumento);
        Task<bool> Existe(string rutNumDocumento);
    }

    public class RepositorioListaBlanca : IRepositorioListaBlanca
    {
        private readonly string connectionString;

        public RepositorioListaBlanca(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }

        private IDbConnection Connection => new SqlConnection(connectionString);

        public async Task<bool> Crear(ListaBlanca modelo)
        {
            using (var db = Connection)
            {
                // Asegúrate que los nombres de las columnas coincidan exactamente con tu tabla
                string query = @"
                    INSERT INTO ftc_ListaBlanca (
                        [RUT/ NUM DE DOCUMENTO], [RUT], [DV], [NUMERO EMPLEADO], [NOMBRES], [APELLIDOS],
                        [APELLIDO_NOMBRE], [NEGOCIO], [PAIS], [CARGO], [DEPARTAMENTO],
                        [TIPO EMPLEADO (INTERNO / EXTERNO)], [ACTIVO_FALANET], [FINIQUITADOS_FALANET], [ACTIVO_AD]
                    ) VALUES (
                        @RutNumDocumento, @RUT, @DV, @NumeroEmpleado, @Nombres, @Apellidos,
                        @ApellidoNombre, @Negocio, @Pais, @Cargo, @Departamento,
                        @TipoEmpleado, @ActivoFalanet, @FiniquitadosFalanet, @ActivoAd
                    );";
                // El campo APELLIDO_NOMBRE podría generarse en la base de datos o aquí.
                // Si es en la BD, no necesitas pasarlo. Si es aquí:
                // modelo.ApellidoNombre = $"{modelo.Apellidos} {modelo.Nombres}".Trim();
                var result = await db.ExecuteAsync(query, modelo);
                return result > 0;
            }
        }

        public async Task<bool> Eliminar(string rutNumDocumento)
        {
            using (var db = Connection)
            {
                // Asegúrate que el nombre de la columna clave coincida
                string query = "DELETE FROM ftc_ListaBlanca WHERE [RUT/ NUM DE DOCUMENTO] = @RutNumDocumento;";
                var result = await db.ExecuteAsync(query, new { RutNumDocumento = rutNumDocumento });
                return result > 0;
            }
        }

        public async Task<IEnumerable<ListaBlanca>> ObtenerTodos()
        {
            using (var db = Connection)
            {
                // Ajusta los nombres de las columnas si es necesario para el mapeo
                return await db.QueryAsync<ListaBlanca>(@"
                    SELECT
                        [RUT/ NUM DE DOCUMENTO] AS RutNumDocumento,
                        [RUT],
                        [DV],
                        [NUMERO EMPLEADO] AS NumeroEmpleado,
                        [NOMBRES],
                        [APELLIDOS],
                        [APELLIDO_NOMBRE] AS ApellidoNombre,
                        [NEGOCIO],
                        [PAIS],
                        [CARGO],
                        [DEPARTAMENTO],
                        [TIPO EMPLEADO (INTERNO / EXTERNO)] AS TipoEmpleado,
                        [ACTIVO_FALANET] AS ActivoFalanet,
                        [FINIQUITADOS_FALANET] AS FiniquitadosFalanet,
                        [ACTIVO_AD] AS ActivoAd
                    FROM ftc_ListaBlanca ORDER BY NOMBRES, APELLIDOS;");
            }
        }

        public async Task<ListaBlanca> ObtenerPorRutNumDocumento(string rutNumDocumento)
        {
            using (var db = Connection)
            {
                return await db.QueryFirstOrDefaultAsync<ListaBlanca>(@"
                    SELECT
                        [RUT/ NUM DE DOCUMENTO] AS RutNumDocumento,
                        [RUT],
                        [DV],
                        [NUMERO EMPLEADO] AS NumeroEmpleado,
                        [NOMBRES],
                        [APELLIDOS],
                        [APELLIDO_NOMBRE] AS ApellidoNombre,
                        [NEGOCIO],
                        [PAIS],
                        [CARGO],
                        [DEPARTAMENTO],
                        [TIPO EMPLEADO (INTERNO / EXTERNO)] AS TipoEmpleado,
                        [ACTIVO_FALANET] AS ActivoFalanet,
                        [FINIQUITADOS_FALANET] AS FiniquitadosFalanet,
                        [ACTIVO_AD] AS ActivoAd
                    FROM ftc_ListaBlanca WHERE [RUT/ NUM DE DOCUMENTO] = @RutNumDocumento;", new { RutNumDocumento = rutNumDocumento });
            }
        }

        public async Task<bool> Actualizar(ListaBlanca modelo)
        {
            using (var db = Connection)
            {
                // modelo.ApellidoNombre = $"{modelo.Apellidos} {modelo.Nombres}".Trim();
                string query = @"
                    UPDATE ftc_ListaBlanca SET
                        [RUT] = @RUT,
                        [DV] = @DV,
                        [NUMERO EMPLEADO] = @NumeroEmpleado,
                        [NOMBRES] = @Nombres,
                        [APELLIDOS] = @Apellidos,
                        [APELLIDO_NOMBRE] = @ApellidoNombre,
                        [NEGOCIO] = @Negocio,
                        [PAIS] = @Pais,
                        [CARGO] = @Cargo,
                        [DEPARTAMENTO] = @Departamento,
                        [TIPO EMPLEADO (INTERNO / EXTERNO)] = @TipoEmpleado,
                        [ACTIVO_FALANET] = @ActivoFalanet,
                        [FINIQUITADOS_FALANET] = @FiniquitadosFalanet,
                        [ACTIVO_AD] = @ActivoAd
                    WHERE [RUT/ NUM DE DOCUMENTO] = @RutNumDocumento;";
                var result = await db.ExecuteAsync(query, modelo);
                return result > 0;
            }
        }

        public async Task<bool> Existe(string rutNumDocumento)
        {
            using (var db = Connection)
            {
                string query = "SELECT COUNT(1) FROM ftc_ListaBlanca WHERE [RUT/ NUM DE DOCUMENTO] = @RutNumDocumento;";
                var count = await db.ExecuteScalarAsync<int>(query, new { RutNumDocumento = rutNumDocumento });
                return count > 0;
            }
        }
    }
}