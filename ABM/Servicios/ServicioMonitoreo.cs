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
    public interface IServicioMonitoreo
    {
        Task RegistrarActividad(string tarea, string tipo, string descripcion, string correoUsuarioOverride = null);
        Task RegistrarActividad(HttpContext httpContext, string tarea, string tipo, string descripcion, string correoUsuarioOverride = null);
    }

    public class ServicioMonitoreo : IServicioMonitoreo
    {
        private readonly IRepositorioMonitoreoLogs _repositorioMonitoreoLogs;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ServicioMonitoreo(
            IRepositorioMonitoreoLogs repositorioMonitoreoLogs,
            IHttpContextAccessor httpContextAccessor)
        {
            _repositorioMonitoreoLogs = repositorioMonitoreoLogs ?? throw new ArgumentNullException(nameof(repositorioMonitoreoLogs));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task RegistrarActividad(string tarea, string tipo, string descripcion, string correoUsuarioOverride = null)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                await RegistrarActividadBase(null, tarea, tipo, descripcion, correoUsuarioOverride);
                return;
            }
            await RegistrarActividadBase(_httpContextAccessor.HttpContext, tarea, tipo, descripcion, correoUsuarioOverride);
        }

        public async Task RegistrarActividad(HttpContext httpContext, string tarea, string tipo, string descripcion, string correoUsuarioOverride = null)
        {
            await RegistrarActividadBase(httpContext, tarea, tipo, descripcion, correoUsuarioOverride);
        }

        private async Task RegistrarActividadBase(HttpContext httpContext, string tarea, string tipo, string descripcion, string correoUsuarioOverride = null)
        {
            if (string.IsNullOrWhiteSpace(tarea))
                throw new ArgumentException("El parámetro 'tarea' no puede ser nulo o vacío.", nameof(tarea));
            if (string.IsNullOrWhiteSpace(tipo))
                throw new ArgumentException("El parámetro 'tipo' no puede ser nulo o vacío.", nameof(tipo));
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new ArgumentException("El parámetro 'descripcion' no puede ser nulo o vacío.", nameof(descripcion));

            string responsable = correoUsuarioOverride;

            if (string.IsNullOrEmpty(responsable) && httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                responsable = httpContext.User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(responsable))
                {
                    // Fallback al nombre de usuario si el claim de email no está o está vacío.
                    responsable = httpContext.User.Identity.Name;
                }
            }

            // Si no se pudo determinar el responsable (ej. acción anónima y no se pasó override)
            if (string.IsNullOrEmpty(responsable))
            {
                responsable = "NoAutenticado/Sistema";
            }

            var log = new MonitoreoLog
            {
                Responsable = responsable,
                Tarea = tarea,
                Tipo = tipo,
                Descripcion = descripcion,
                Timestamp = DateTime.Now // Se asigna aquí, el repositorio tiene un fallback por si acaso.
            };

            try
            {
                await _repositorioMonitoreoLogs.RegistrarLog(log);
            }
            catch (Exception ex)
            {
                throw; // Relanza la excepción para mantener la trazabilidad.
            }
        }
    }
}
