using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace ABM.Filters
{
    public class DatabaseExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is SqlException)
            {
                Debug.WriteLine("⚠ Error de base de datos detectado. Redirigiendo a Login.");

                context.Result = new RedirectToActionResult("Login", "Acceso", null);
                context.ExceptionHandled = true;
            }
        }
    }
}
