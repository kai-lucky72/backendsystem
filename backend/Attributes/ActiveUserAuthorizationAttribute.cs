using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace backend.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ActiveUserAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is active from JWT claims
        var activeClaim = user.FindFirst("Active");
        if (activeClaim == null || !bool.TryParse(activeClaim.Value, out bool isActive) || !isActive)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Account is deactivated. Please contact administrator." });
            return;
        }
    }
} 