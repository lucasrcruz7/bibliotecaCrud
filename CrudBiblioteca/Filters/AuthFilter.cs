using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AuthFilter : ActionFilterAttribute
{
    // Rotas liberadas sem login
    private static readonly (string controller, string action)[] RotasPublicas =
    {
        ("Usuarios", "Login"),
        ("Usuarios", "Create"),
        ("Home",     "Index"),
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var routeData = context.RouteData;
        var controller = routeData.Values["controller"]?.ToString() ?? "";
        var action = routeData.Values["action"]?.ToString() ?? "";

        bool ehPublica = Array.Exists(RotasPublicas,
            r => r.controller.Equals(controller, StringComparison.OrdinalIgnoreCase) &&
                 r.action.Equals(action, StringComparison.OrdinalIgnoreCase));

        if (ehPublica)
        {
            base.OnActionExecuting(context);
            return;
        }

        var session = context.HttpContext.Session;
        bool logado = session.GetInt32("UsuarioId").HasValue;

        if (!logado)
        {
            context.Result = new RedirectToActionResult("Login", "Usuarios", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}