using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AI_Order.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireManagementKeyAttribute : ActionFilterAttribute
{
    private const string HeaderName = "X-Management-Key";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = config["ManagementApiKey"];

        if (string.IsNullOrEmpty(expected))
        {
            context.Result = new ObjectResult(new { error = "ManagementApiKey is not configured on the API." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided)
            || !string.Equals(provided, expected, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
