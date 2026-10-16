using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ABM.Servicios;

namespace ABM.Helpers
{
    public static class ValidacionDeSesion
    {
        // Se engancha en CookieAuthenticationEvents.OnValidatePrincipal, que corre en cada petición con cookie.
        // Si la cuenta de esa sesión fue deshabilitada, se rechaza la sesión y se borra la cookie: la siguiente
        // petición vuelve al login. El estado sale de EstadoUsuarioService (caché corto, invalidado al cambiarlo).
        public static async Task CerrarSiCuentaDeshabilitadaAsync(CookieValidatePrincipalContext contexto)
        {
            if (!int.TryParse(contexto.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idUsuario))
            {
                return;
            }

            var estados = contexto.HttpContext.RequestServices.GetRequiredService<IEstadoUsuarioService>();
            if (!await estados.EstaHabilitadoAsync(idUsuario))
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
