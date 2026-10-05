using Microsoft.AspNetCore.Mvc.Controllers;

namespace ABM.Helpers
{
    // Cabeceras de seguridad HTTP para todas las respuestas (no hay WAF ni balanceador que las agregue).
    //
    // - Content-Security-Policy (bloqueante): sólo las directivas que no dependen del JavaScript inline de las vistas.
    // - Content-Security-Policy-Report-Only: la política estricta, para catalogar en la consola del navegador qué
    //   scripts/estilos inline y qué orígenes externos hay que ajustar antes de pasarla a modo bloqueo.
    // - X-Frame-Options no se agrega acá: ASP.NET la agrega solo (SAMEORIGIN) en las páginas con antiforgery,
    //   que es la única que vio el informe; duplicarla invalidaría el valor. frame-ancestors la cubre en el resto.
    // - Strict-Transport-Security queda apagada hasta corregir el certificado TLS: se activa con "Seguridad:Hsts": true.
    public class CabecerasSeguridadMiddleware
    {
        private const string CspBloqueante =
            "frame-ancestors 'self'; base-uri 'self'; form-action 'self'; object-src 'none'";

        private const string CspReportOnly =
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://code.jquery.com https://unpkg.com https://cdn.datatables.net; " +
            "style-src 'self' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net; " +
            "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
            "img-src 'self' data: blob:; connect-src 'self'; " +
            "frame-ancestors 'self'; base-uri 'self'; form-action 'self'; object-src 'none'";

        private const string PermissionsPolicy =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        private readonly RequestDelegate _next;
        private readonly bool _hsts;

        public CabecerasSeguridadMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _hsts = configuration.GetValue<bool>("Seguridad:Hsts");
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var cabeceras = context.Response.Headers;

                cabeceras["X-Content-Type-Options"] = "nosniff";
                cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
                cabeceras["Permissions-Policy"] = PermissionsPolicy;
                cabeceras["Cross-Origin-Opener-Policy"] = "same-origin";
                cabeceras["Cross-Origin-Resource-Policy"] = "same-origin";
                cabeceras["X-Permitted-Cross-Domain-Policies"] = "none";
                cabeceras["Content-Security-Policy"] = CspBloqueante;
                cabeceras["Content-Security-Policy-Report-Only"] = CspReportOnly;

                if (_hsts && context.Request.IsHttps)
                {
                    cabeceras["Strict-Transport-Security"] = "max-age=31536000";
                }

                // Las páginas y respuestas dinámicas de usuarios autenticados llevan datos personales: no se cachean.
                var esAccionMvc = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() != null;
                if (esAccionMvc && context.User.Identity?.IsAuthenticated == true && !cabeceras.ContainsKey("Cache-Control"))
                {
                    cabeceras["Cache-Control"] = "no-store";
                    cabeceras["Pragma"] = "no-cache";
                }

                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
