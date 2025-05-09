using Microsoft.AspNetCore.Mvc;

namespace ABM.Filters
{
    public class MonitoreoAttribute : TypeFilterAttribute
    {
        public MonitoreoAttribute(string tarea, string tipo, string descripcion)
            : base(typeof(MonitoreoFilter))
        {
            Arguments = new object[] { tarea, tipo, descripcion };
        }
    }
}
