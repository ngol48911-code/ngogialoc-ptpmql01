using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using MvcMovie.Controllers;
using MvcMovie.Models;

namespace MvcMovie.Services;

public sealed class PermissionScannerService
{
    public const string PermissionClaimType = "Permission";

    public IReadOnlyList<ControllerPermissionGroup> GetControllerPermissions(
        IEnumerable<string>? selectedPermissions = null)
    {
        var selectedClaims = new HashSet<string>(
            selectedPermissions ?? [],
            StringComparer.Ordinal);

        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && typeof(Controller).IsAssignableFrom(type)
                && !IsExcludedController(type))
            .Select(type =>
            {
                var controllerName = GetControllerName(type);
                var actions = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(IsActionMethod)
                    .Select(method => method.GetCustomAttribute<ActionNameAttribute>(inherit: true)?.Name
                        ?? method.Name)
                    .Where(actionName => !string.IsNullOrWhiteSpace(actionName))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(actionName => actionName, StringComparer.OrdinalIgnoreCase)
                    .Select(actionName =>
                    {
                        var claimValue = $"{controllerName}.{actionName}";
                        return new ActionPermissionItem(
                            actionName,
                            claimValue,
                            selectedClaims.Contains(claimValue));
                    })
                    .ToArray();

                return new ControllerPermissionGroup(controllerName, actions);
            })
            .Where(group => group.Actions.Count > 0)
            .OrderBy(group => group.ControllerName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        return method.IsPublic
            && !method.IsStatic
            && !method.IsAbstract
            && !method.IsSpecialName
            && !method.ContainsGenericParameters
            && method.DeclaringType != typeof(object)
            && method.DeclaringType != typeof(Controller)
            && method.DeclaringType != typeof(ControllerBase)
            && method.GetCustomAttribute<NonActionAttribute>(inherit: true) == null;
    }

    private static bool IsExcludedController(Type type)
    {
        var controllerName = GetControllerName(type);
        return string.Equals(controllerName, nameof(HomeController).Replace("Controller", string.Empty), StringComparison.Ordinal)
            || string.Equals(controllerName, nameof(RoleController).Replace("Controller", string.Empty), StringComparison.Ordinal)
            || string.Equals(controllerName, nameof(UserController).Replace("Controller", string.Empty), StringComparison.Ordinal);
    }

    private static string GetControllerName(Type type)
    {
        return type.Name.EndsWith("Controller", StringComparison.Ordinal)
            ? type.Name[..^"Controller".Length]
            : type.Name;
    }
}
