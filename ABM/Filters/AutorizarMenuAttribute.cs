using Microsoft.AspNetCore.Mvc;

namespace ABM.Filters
{
    // Exige en el servidor que el rol del usuario tenga permitido (ftc_PermisosMenu) un menú o submenú
    // del controller decorado. Hasta ahora esa restricción sólo ocultaba los links del sidebar.
    public class AutorizarMenuAttribute : TypeFilterAttribute
    {
        public AutorizarMenuAttribute() : base(typeof(AutorizarMenuFilter))
        {
        }
    }
}
