using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Tests;

public class UserControllerTests
{
    [Fact]
    public void User_controller_requires_admin_and_antiforgery_for_role_updates()
    {
        var authorize = typeof(UserController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", authorize?.Roles);

        var action = typeof(UserController).GetMethod(nameof(UserController.UpdateUserRoles));
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task UpdateUserRoles_rejects_missing_antiforgery_header()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Controller.ControllerContext.HttpContext.Request.Headers.Remove("RequestVerificationToken");

        var result = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.UpdateUserRoles(new UpdateUserRolesRequest
            {
                UserId = "user-id",
                Roles = []
            }));

        Assert.Contains("RequestVerificationToken", GetMessage(result.Value));
    }

    [Fact]
    public async Task GetUsers_filters_by_email_and_role_before_paging()
    {
        await using var fixture = await CreateFixtureAsync();
        await fixture.CreateRoleAsync("Editor");
        await fixture.CreateRoleAsync("Viewer");
        var firstEditor = await fixture.CreateUserAsync("alex.one@example.test", "Editor");
        var secondEditor = await fixture.CreateUserAsync("alex.two@example.test", "Editor");
        await fixture.CreateUserAsync("alex.three@example.test", "Viewer");
        await fixture.CreateUserAsync("other@example.test", "Editor");

        var first = GetPage(await fixture.Controller.GetUsers("ALEX", "editor", 1, 1));
        var second = GetPage(await fixture.Controller.GetUsers("ALEX", "editor", 2, 1));

        Assert.Equal(2, first.TotalItems);
        Assert.Equal(2, first.TotalPages);
        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.Contains(first.Items[0].Id, new[] { firstEditor.Id, secondEditor.Id });
        Assert.Contains(second.Items[0].Id, new[] { firstEditor.Id, secondEditor.Id });
        Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
        Assert.Contains("Editor", first.Items[0].Roles);
    }

    [Fact]
    public async Task UpdateUserRoles_adds_selected_roles_and_removes_unselected_roles()
    {
        await using var fixture = await CreateFixtureAsync();
        await fixture.CreateRoleAsync("OldRole");
        await fixture.CreateRoleAsync("NewRole");
        await fixture.CreateRoleAsync("ExtraRole");
        var user = await fixture.CreateUserAsync("member@example.test", "OldRole");

        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.UpdateUserRoles(
            new UpdateUserRolesRequest
            {
                UserId = user.Id,
                Roles = ["NewRole", "ExtraRole"]
            }));
        var response = Assert.IsType<UserRoleManageDto>(result.Value);
        var assignedRoles = await fixture.UserManager.GetRolesAsync(user);

        Assert.DoesNotContain("OldRole", assignedRoles);
        Assert.Contains("NewRole", assignedRoles);
        Assert.Contains("ExtraRole", assignedRoles);
        Assert.All(
            response.Roles.Where(role => role.IsSelected),
            role => Assert.Contains(role.Name, assignedRoles));
    }

    [Fact]
    public async Task UpdateUserRoles_rejects_admin_removing_admin_from_self()
    {
        await using var fixture = await CreateFixtureAsync();
        await fixture.CreateRoleAsync("Admin");
        var admin = await fixture.CreateUserAsync("admin@example.test", "Admin");
        fixture.SetAdminPrincipal(admin.Id);

        var result = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.UpdateUserRoles(new UpdateUserRolesRequest
            {
                UserId = admin.Id,
                Roles = []
            }));

        Assert.Contains("không thể tự gỡ quyền Admin", GetMessage(result.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Admin", await fixture.UserManager.GetRolesAsync(admin));
    }

    private static PagedResult<UserResponseDto> GetPage(IActionResult result)
    {
        return Assert.IsType<PagedResult<UserResponseDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
    }

    private static string GetMessage(object? value)
    {
        Assert.NotNull(value);
        return (string)value!.GetType().GetProperty("message")!.GetValue(value)!;
    }

    private static async Task<UserControllerFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"MvcMovieUserTests-{Guid.NewGuid()}"));
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        return new UserControllerFixture(
            provider,
            scope,
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    }

    private sealed class UserControllerFixture(
        ServiceProvider provider,
        IServiceScope scope,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager) : IAsyncDisposable
    {
        public UserManager<IdentityUser> UserManager { get; } = userManager;
        public RoleManager<IdentityRole> RoleManager { get; } = roleManager;
        public UserController Controller { get; } = CreateController(userManager, roleManager);

        public async Task CreateRoleAsync(string roleName)
        {
            Assert.True((await RoleManager.CreateAsync(new IdentityRole(roleName))).Succeeded);
        }

        public async Task<IdentityUser> CreateUserAsync(string email, params string[] roles)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            Assert.True((await UserManager.CreateAsync(user, "Pass123!")).Succeeded);
            if (roles.Length > 0)
            {
                Assert.True((await UserManager.AddToRolesAsync(user, roles)).Succeeded);
            }
            return user;
        }

        public void SetAdminPrincipal(string userId)
        {
            Controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, "Admin")
                ],
                "Test"));
        }

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            provider.Dispose();
            return ValueTask.CompletedTask;
        }

        private static UserController CreateController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            var controller = new UserController(userManager, roleManager);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Headers["RequestVerificationToken"] = "test-token";
            return controller;
        }
    }
}
