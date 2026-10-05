using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace ABM.Servicios
{
    public interface IRepositorioDescargas
    {
        Task<IEnumerable<dynamic>> ObtenerDatosFiniquitados();
        Task<IEnumerable<dynamic>> ObtenerDatosActivosFalanet();
        Task<IEnumerable<dynamic>> ObtenerDatosAD(int idUsuario);
    }

    public class RepositorioDescargas : IRepositorioDescargas
    {
        private readonly string connectionString;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RepositorioDescargas> _logger;

        // Lista blanca de atributos de AD que se exportan (mínimo necesario para el flujo de altas y bajas).
        // Se puede reemplazar desde appsettings.json: "ExportacionAD": { "Columnas": [ "mail", "employeeID", ... ] }
        // Quedan fuera a propósito: info/description (texto libre donde terminan claves), certificados, fotos,
        // contadores de intentos fallidos, servicePrincipalName, adminCount y memberOf.
        private static readonly string[] ColumnasADPorDefecto =
        {
            "sAMAccountName", "userPrincipalName", "mail", "employeeID", "displayName", "givenName", "sn", "cn",
            "title", "department", "company", "physicalDeliveryOfficeName", "c", "co", "countryCode",
            "userAccountControl", "accountExpires", "whenCreated", "whenChanged", "lastLogon", "lastLogonTimestamp", "manager"
        };

        public RepositorioDescargas(IConfiguration configuration, ILogger<RepositorioDescargas> logger)
        {
            _configuration = configuration;
            _logger = logger;
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

        public async Task<IEnumerable<dynamic>> ObtenerDatosAD(int idUsuario)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Alcance del operador, calculado en el servidor: los países de los PNS asignados a su rol.
                // ftc_ad.c y ftc_pns_2.codPais usan el mismo código de 2 letras (CL, AR, ...). "SP" (SIN PAIS)
                // habilita además los objetos de AD sin país informado.
                var paisesDelRol = (await db.QueryAsync<string>(@"
                    SELECT DISTINCT UPPER(LTRIM(RTRIM(pns.codPais)))
                    FROM ftc_usuario u
                    JOIN ftc_detalle_rol dr ON dr.idRol = u.idRol
                    JOIN ftc_pns_2 pns ON pns.idPaisNegocioSistema = dr.idPaisNegocioSistema
                    WHERE u.idUsuario = @idUsuario AND pns.codPais IS NOT NULL",
                    new { idUsuario })).ToList();

                var incluirSinPais = paisesDelRol.Contains("SP");
                var codigosPais = paisesDelRol.Where(p => p != "SP").ToArray();

                if (codigosPais.Length == 0 && !incluirSinPais)
                {
                    _logger.LogWarning("Export AD: el usuario {IdUsuario} no tiene países asignados en su rol; no se exporta nada.", idUsuario);
                    return new List<dynamic>();
                }

                // Se agrega GETDATE() como Fecha_Descarga
                var query = @"
                    SELECT *,
                           FORMAT(GETDATE(), 'dd-MM-yyyy HH:mm:ss') AS Fecha_Descarga
                    FROM ftc_ad
                    WHERE UPPER(LTRIM(RTRIM(c))) IN @codigosPais
                       OR (@incluirSinPais = 1 AND (c IS NULL OR LTRIM(RTRIM(c)) = ''));";

                var filas = (await db.QueryAsync<dynamic>(query, new { codigosPais, incluirSinPais = incluirSinPais ? 1 : 0 }))
                    .Cast<IDictionary<string, object>>().ToList();

                _logger.LogInformation("Export AD: usuario {IdUsuario}, países {Paises}, sin país {SinPais}, filas {Filas}.",
                    idUsuario, string.Join(",", codigosPais), incluirSinPais, filas.Count);
                if (filas.Count == 0)
                {
                    return filas;
                }

                // Se proyecta sólo la lista blanca (sin distinguir mayúsculas). Si una columna configurada no existe
                // en la tabla se ignora, así que un nombre mal escrito nunca rompe la consulta.
                var configuradas = _configuration.GetSection("ExportacionAD:Columnas").Get<string[]>();
                var solicitadas = configuradas is { Length: > 0 } ? configuradas : ColumnasADPorDefecto;

                var clavesReales = filas[0].Keys.ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);
                var columnas = solicitadas
                    .Where(c => clavesReales.ContainsKey(c))
                    .Select(c => clavesReales[c])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var faltantes = solicitadas.Where(c => !clavesReales.ContainsKey(c)).ToList();
                if (faltantes.Count > 0)
                {
                    _logger.LogWarning("Export AD: columnas de la lista blanca que no existen en ftc_ad: {Columnas}", string.Join(", ", faltantes));
                }

                if (columnas.Count == 0)
                {
                    _logger.LogError("Export AD: ninguna columna de la lista blanca existe en ftc_ad; no se exporta nada.");
                    return new List<dynamic>();
                }

                var resultado = new List<dynamic>(filas.Count);
                foreach (var fila in filas)
                {
                    IDictionary<string, object> salida = new ExpandoObject();
                    foreach (var columna in columnas)
                    {
                        salida[columna] = fila[columna];
                    }
                    salida["Fecha_Descarga"] = fila["Fecha_Descarga"];
                    resultado.Add(salida);
                }

                return resultado;
            }
        }
    }
}