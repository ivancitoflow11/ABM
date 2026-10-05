using System.Security.Claims;
using ABM.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ABM.Filters
{
    public class AutorizarMenuFilter : IAsyncAuthorizationFilter
    {
        private readonly IRepositorioMenus _repositorioMenus;
        private readonly IServicioMonitoreo _servicioMonitoreo;
        private readonly ILogger<AutorizarMenuFilter> _logger;

        public AutorizarMenuFilter(
            IRepositorioMenus repositorioMenus,
            IServicioMonitoreo servicioMonitoreo,
            ILogger<AutorizarMenuFilter> logger)
        {
            _repositorioMenus = repositorioMenus;
            _servicioMonitoreo = servicioMonitoreo;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var controlador = descriptor?.ControllerName;
            var accion = descriptor?.ActionName;

            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario);

            var permitido = idUsuario > 0
                && !string.IsNullOrEmpty(controlador)
                && await _repositorioMenus.UsuarioTieneAccesoAControlador(idUsuario, controlador);

            if (permitido)
            {
                return;
            }

            _logger.LogWarning("Acceso denegado a {Controlador}/{Accion} para el usuario {IdUsuario}.", controlador, accion, idUsuario);

            try
            {
                await _servicioMonitoreo.RegistrarActividad("AccesoDenegado", "SELECT", $"{controlador}/{accion}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo registrar el acceso denegado en el monitoreo.");
            }

            var request = context.HttpContext.Request;
            var esAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (!HttpMethods.IsGet(request.Method) || esAjax)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            // Un filtro de autorización que corta el pipeline no pasa por SaveTempDataFilter: se guarda a mano.
            var tempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            tempData["ErrorMessage"] = "No tiene permisos para acceder a esa sección.";
            tempData.Save();

            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
