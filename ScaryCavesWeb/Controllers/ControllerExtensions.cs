using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;

namespace ScaryCavesWeb.Controllers;

public static class ControllerExtensions
{
    public static IActionResult RedirectTo<TController>(this TController controller, Expression<Func<TController, Task<IActionResult>>> action, object? routeValues = null) where TController : Controller
    {
        // Get the method info for the action
        if (action.Body is MethodCallExpression methodCall)
        {
            string actionName = methodCall.Method.Name;
            // Get the controller name without the "Controller" suffix
            string controllerName = typeof(TController).Name.Replace("Controller", "");

            return controller.RedirectToAction(actionName, controllerName, routeValues);
        }
        throw new ArgumentException("Expression must be a method call");
    }
}
