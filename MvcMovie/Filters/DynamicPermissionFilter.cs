using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;

namespace MvcMovie.Filters;

public sealed class DynamicPermissionFilter : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> ExcludedControllers =
        new(StringComparer.OrdinalIgnoreCase) { "Home", "Role", "User" };

    private readonly RoleManager<IdentityRole> _roleManager;

    public DynamicPermissionFilter(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Filters.OfType<IAllowAnonymousFilter>().Any()
            || string.Equals(
                context.RouteData.Values["area"]?.ToString(),
                "Identity",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString();
        if (string.IsNullOrWhiteSpace(controllerName)
            || ExcludedControllers.Contains(controllerName))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (user.IsInRole("Admin"))
        {
            return;
        }

        var actionName = context.RouteData.Values["action"]?.ToString();
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return;
        }

        var expectedPermission = $"{controllerName}.{actionName}";
        var roleClaimTypes = user.Identities
            .Select(identity => identity.RoleClaimType)
            .ToHashSet(StringComparer.Ordinal);
        var roleNames = user.Claims
            .Where(claim => roleClaimTypes.Contains(claim.Type))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            if (claims.Any(claim =>
                claim.Type == "Permission"
                && string.Equals(claim.Value, expectedPermission, StringComparison.Ordinal)))
            {
                return;
            }
        }

        if (IsAjaxRequest(context.HttpContext.Request))
        {
            context.Result = new JsonResult(new
            {
                message = "Bạn không có quyền thực hiện chức năng này!"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        context.Result = new RedirectResult("/Identity/Account/AccessDenied");
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        var acceptHeader = request.Headers.Accept.ToString();
        var path = request.Path.Value ?? string.Empty;
        return acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Ajax", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Get", StringComparison.OrdinalIgnoreCase);
    }
}
