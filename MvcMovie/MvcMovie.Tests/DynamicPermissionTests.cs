using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Filters;
using MvcMovie.Models;
using MvcMovie.Services;

namespace MvcMovie.Tests;

public class DynamicPermissionTests
{
    [Fact]
    public void Permission_scanner_finds_data_controller_actions_in_sorted_groups()
    {
        var groups = new PermissionScannerService().GetControllerPermissions();
        var controllerNames = groups.Select(group => group.ControllerName).ToArray();

        Assert.Contains("Student", controllerNames);
        Assert.Contains("Person", controllerNames);
        Assert.Contains("Movie", controllerNames);
        Assert.DoesNotContain("Home", controllerNames);
        Assert.DoesNotContain("Role", controllerNames);
        Assert.DoesNotContain("User", controllerNames);
        Assert.Equal(
            controllerNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
            controllerNames);

        AssertControllerActions(groups, "Person", "Index", "GetPersons", "GetPersonById",
            "CreateAjax", "EditAjax", "DeleteAjax", "Create", "Edit", "Delete");
        AssertControllerActions(groups, "Movie", "Index", "GetMovies", "GetMovieById",
            "CreateAjax", "EditAjax", "DeleteAjax");

        var studentActions = groups.Single(group => group.ControllerName == "Student")
            .Actions.Select(action => action.ActionName).ToArray();
        Assert.Contains("Index", studentActions);
        Assert.Contains("GetStudents", studentActions);
        Assert.Contains("GetStudentById", studentActions);
        Assert.Contains("CreateAjax", studentActions);
        Assert.Contains("EditAjax", studentActions);
        Assert.Contains("DeleteAjax", studentActions);
        Assert.DoesNotContain("GetType", studentActions);
        Assert.Equal(
            studentActions.Length,
            groups.Single(group => group.ControllerName == "Student")
                .Actions.Select(action => action.ClaimValue).Distinct().Count());
    }

    [Fact]
    public async Task UpdateRolePermissions_replaces_permission_claims_and_preserves_other_claims()
    {
        await using var fixture = await CreateFixtureAsync();
        var role = await fixture.CreateRoleAsync("Editor");
        await fixture.RoleManager.AddClaimAsync(role, new Claim("department", "science"));

        var firstUpdate = Assert.IsType<OkObjectResult>(
            await fixture.Controller.UpdateRolePermissions(new UpdateRolePermissionsRequest
            {
                RoleId = role.Id,
                Permissions = ["Student.Index", "Person.CreateAjax"]
            }));
        Assert.NotNull(firstUpdate.Value);
        var permissionView = Assert.IsType<RolePermissionManageDto>(
            Assert.IsType<OkObjectResult>(
                await fixture.Controller.GetRolePermissions(role.Id)).Value);
        Assert.Contains(
            permissionView.Controllers.Single(group => group.ControllerName == "Student")
                .Actions,
            action => action.ClaimValue == "Student.Index" && action.IsSelected);
        Assert.Contains(
            permissionView.Controllers.Single(group => group.ControllerName == "Person")
                .Actions,
            action => action.ClaimValue == "Person.CreateAjax" && action.IsSelected);

        var secondUpdate = Assert.IsType<OkObjectResult>(
            await fixture.Controller.UpdateRolePermissions(new UpdateRolePermissionsRequest
            {
                RoleId = role.Id,
                Permissions = ["Movie.GetMovies"]
            }));
        var claims = await fixture.RoleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(claim => claim.Type == PermissionScannerService.PermissionClaimType)
            .Select(claim => claim.Value)
            .ToArray();

        Assert.NotNull(secondUpdate.Value);
        Assert.Equal(["Movie.GetMovies"], permissions);
        Assert.Contains(claims, claim => claim.Type == "department" && claim.Value == "science");
    }

    [Fact]
    public async Task UpdateRolePermissions_rejects_admin_role()
    {
        await using var fixture = await CreateFixtureAsync();
        var role = await fixture.CreateRoleAsync("Admin");

        var result = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.UpdateRolePermissions(new UpdateRolePermissionsRequest
            {
                RoleId = role.Id,
                Permissions = ["Student.Index"]
            }));

        Assert.Contains("toàn quyền mặc định", GetMessage(result.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await fixture.RoleManager.GetClaimsAsync(role));
    }

    [Fact]
    public async Task Dynamic_permission_filter_allows_claimed_role_and_admin()
    {
        await using var fixture = await CreateFixtureAsync();
        var editor = await fixture.CreateRoleAsync("Editor");
        await fixture.RoleManager.AddClaimAsync(
            editor,
            new Claim(PermissionScannerService.PermissionClaimType, "Student.CreateAjax"));
        var filter = new DynamicPermissionFilter(fixture.RoleManager);
        var permittedContext = CreateContext("Student", "CreateAjax", CreatePrincipal("Editor"));

        await filter.OnAuthorizationAsync(permittedContext);

        Assert.Null(permittedContext.Result);

        var adminContext = CreateContext("Student", "DeleteAjax", CreatePrincipal("Admin"));
        await filter.OnAuthorizationAsync(adminContext);
        Assert.Null(adminContext.Result);
    }

    [Fact]
    public async Task Dynamic_permission_filter_returns_ajax_403_or_regular_access_denied_redirect()
    {
        await using var fixture = await CreateFixtureAsync();
        var filter = new DynamicPermissionFilter(fixture.RoleManager);
        var ajaxContext = CreateContext("Student", "CreateAjax", CreatePrincipal("Editor"));
        ajaxContext.HttpContext.Request.Headers.Accept = "application/json";

        await filter.OnAuthorizationAsync(ajaxContext);

        var forbidden = Assert.IsType<JsonResult>(ajaxContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal(
            "Bạn không có quyền thực hiện chức năng này!",
            GetMessage(forbidden.Value));

        var browserContext = CreateContext("Movie", "Edit", CreatePrincipal("Editor"));
        await filter.OnAuthorizationAsync(browserContext);

        var redirect = Assert.IsType<RedirectResult>(browserContext.Result);
        Assert.Equal("/Identity/Account/AccessDenied", redirect.Url);
    }

    [Fact]
    public async Task Dynamic_permission_filter_skips_home_identity_and_management_controllers()
    {
        await using var fixture = await CreateFixtureAsync();
        var filter = new DynamicPermissionFilter(fixture.RoleManager);
        var homeContext = CreateContext("Home", "Index", CreatePrincipal("Editor"));
        var roleContext = CreateContext("Role", "UpdateRolePermissions", CreatePrincipal("Editor"));
        var userContext = CreateContext("User", "UpdateUserRoles", CreatePrincipal("Editor"));
        var identityContext = CreateContext("Account", "Login", CreatePrincipal("Editor"));
        identityContext.RouteData.Values["area"] = "Identity";

        await filter.OnAuthorizationAsync(homeContext);
        await filter.OnAuthorizationAsync(roleContext);
        await filter.OnAuthorizationAsync(userContext);
        await filter.OnAuthorizationAsync(identityContext);

        Assert.Null(homeContext.Result);
        Assert.Null(roleContext.Result);
        Assert.Null(userContext.Result);
        Assert.Null(identityContext.Result);
    }

    private static AuthorizationFilterContext CreateContext(
        string controller,
        string action,
        ClaimsPrincipal principal)
    {
        var httpContext = new DefaultHttpContext { User = principal };
        var routeData = new RouteData();
        routeData.Values["controller"] = controller;
        routeData.Values["action"] = action;
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }

    private static ClaimsPrincipal CreatePrincipal(string roleName)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-id"),
            new Claim(ClaimTypes.Role, roleName)
        ],
        "Test"));
    }

    private static string GetMessage(object? value)
    {
        Assert.NotNull(value);
        return (string)value!.GetType().GetProperty("message")!.GetValue(value)!;
    }

    private static void AssertControllerActions(
        IReadOnlyList<ControllerPermissionGroup> groups,
        string controllerName,
        params string[] expectedActions)
    {
        var actions = groups.Single(group => group.ControllerName == controllerName)
            .Actions.Select(action => action.ActionName);
        Assert.All(expectedActions, action => Assert.Contains(action, actions));
    }

    private static async Task<PermissionTestFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"MvcMoviePermissionTests-{Guid.NewGuid()}"));
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<PermissionScannerService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        return new PermissionTestFixture(
            provider,
            scope,
            roleManager,
            userManager,
            new PermissionScannerService());
    }

    private sealed class PermissionTestFixture(
        ServiceProvider provider,
        IServiceScope scope,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        PermissionScannerService permissionScanner) : IAsyncDisposable
    {
        public RoleManager<IdentityRole> RoleManager { get; } = roleManager;
        public RoleController Controller { get; } = new(roleManager, userManager, permissionScanner);

        public async Task<IdentityRole> CreateRoleAsync(string name)
        {
            var role = new IdentityRole(name);
            Assert.True((await RoleManager.CreateAsync(role)).Succeeded);
            return role;
        }

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
