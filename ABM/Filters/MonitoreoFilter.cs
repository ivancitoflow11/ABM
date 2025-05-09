using ABM.Servicios;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace ABM.Filters
{
    public class MonitoreoFilter : IAsyncActionFilter
    {
        private readonly string _tarea;
        private readonly string _tipo;
        private readonly string _descripcion;
        private readonly IServicioMonitoreo _servicioMonitoreo;

        public MonitoreoFilter(
            string tarea,
            string tipo,
            string descripcion,
            IServicioMonitoreo servicioMonitoreo)
        {
            _tarea = tarea;
            _tipo = tipo;
            _descripcion = descripcion;
            _servicioMonitoreo = servicioMonitoreo;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // registrar antes de ejecutar la acción
            await _servicioMonitoreo.RegistrarActividad(_tarea, _tipo, _descripcion);
            await next();
        }
    }
}
