using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Models;
using MvcMovie.Services;

namespace MvcMovie.Tests;

public class RoleControllerTests
{
    [Fact]
    public void Role_controller_requires_admin_and_antiforgery_for_mutations()
    {
        var authorize = typeof(RoleController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", authorize?.Roles);

        foreach (var actionName in new[] { "CreateAjax", "EditAjax", "DeleteAjax" })
        {
            var action = typeof(RoleController).GetMethod(actionName);
            Assert.NotNull(action);
            Assert.NotNull(action!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }

    [Fact]
    public async Task GetRoles_searches_case_insensitively_and_pages_results()
    {
        await using var fixture = await CreateFixtureAsync();
        await fixture.RoleManager.CreateAsync(new IdentityRole("Editor"));
        await fixture.RoleManager.CreateAsync(new IdentityRole("Senior Editor"));
        await fixture.RoleManager.CreateAsync(new IdentityRole("Reviewer"));

        var result = await fixture.Controller.GetRoles("EDITOR", 1, 1);
        var page = GetPage(result);

        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
        Assert.Equal("Editor", page.Items[0].Name);
        Assert.Equal(1, page.PageIndex);
    }

    [Fact]
    public async Task CreateAjax_creates_role_and_rejects_duplicate_or_blank_name()
    {
        await using var fixture = await CreateFixtureAsync();

        var created = Assert.IsType<OkObjectResult>(
            await fixture.Controller.CreateAjax(new RoleDto { Name = "  Auditor  " }));
        var createdRole = Assert.IsType<RoleResponseDto>(created.Value);
        Assert.Equal("Auditor", createdRole.Name);

        var duplicate = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.CreateAjax(new RoleDto { Name = "auditor" }));
        Assert.Contains("errors", duplicate.Value!.GetType().GetProperties().Select(p => p.Name));

        var blank = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.CreateAjax(new RoleDto { Name = "   " }));
        Assert.Contains("errors", blank.Value!.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task EditAjax_updates_custom_role_and_rejects_system_roles()
    {
        await using var fixture = await CreateFixtureAsync();
        var customRole = new IdentityRole("Moderator");
        await fixture.RoleManager.CreateAsync(customRole);
        var adminRole = new IdentityRole("Admin");
        await fixture.RoleManager.CreateAsync(adminRole);
        var userRole = new IdentityRole("User");
        await fixture.RoleManager.CreateAsync(userRole);

        var updated = Assert.IsType<OkObjectResult>(
            await fixture.Controller.EditAjax(customRole.Id, new RoleDto { Name = "  Reviewer " }));
        Assert.Equal("Reviewer", Assert.IsType<RoleResponseDto>(updated.Value).Name);

        var admin = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.EditAjax(adminRole.Id, new RoleDto { Name = "Root" }));
        var user = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.EditAjax(userRole.Id, new RoleDto { Name = "Member" }));
        Assert.Contains("hệ thống", GetMessage(admin.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hệ thống", GetMessage(user.Value), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAjax_deletes_empty_role_and_rejects_system_or_assigned_role()
    {
        await using var fixture = await CreateFixtureAsync();
        var emptyRole = new IdentityRole("Temporary");
        await fixture.RoleManager.CreateAsync(emptyRole);
        var adminRole = new IdentityRole("Admin");
        await fixture.RoleManager.CreateAsync(adminRole);
        var userRole = new IdentityRole("User");
        await fixture.RoleManager.CreateAsync(userRole);
        var assignedRole = new IdentityRole("Editor");
        await fixture.RoleManager.CreateAsync(assignedRole);

        var user = new IdentityUser { UserName = "role-user@example.test" };
        Assert.True((await fixture.UserManager.CreateAsync(user, "Test123!")).Succeeded);
        Assert.True((await fixture.UserManager.AddToRoleAsync(user, assignedRole.Name!)).Succeeded);

        Assert.IsType<OkObjectResult>(await fixture.Controller.DeleteAjax(emptyRole.Id));
        var adminDelete = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.DeleteAjax(adminRole.Id));
        var userDelete = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.DeleteAjax(userRole.Id));
        var assignedDelete = Assert.IsType<BadRequestObjectResult>(
            await fixture.Controller.DeleteAjax(assignedRole.Id));

        Assert.Contains("hệ thống", GetMessage(adminDelete.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hệ thống", GetMessage(userDelete.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 người dùng", GetMessage(assignedDelete.Value));
        Assert.Null(await fixture.RoleManager.FindByIdAsync(emptyRole.Id));
        Assert.NotNull(await fixture.RoleManager.FindByIdAsync(assignedRole.Id));
    }

    private static PagedResult<RoleResponseDto> GetPage(IActionResult result)
    {
        return Assert.IsType<PagedResult<RoleResponseDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
    }

    private static string GetMessage(object? value)
    {
        Assert.NotNull(value);
        return (string)value!.GetType().GetProperty("message")!.GetValue(value)!;
    }

    private static async Task<RoleControllerFixture> CreateFixtureAsync()
    {
        var databaseName = $"MvcMovieRoleTests-{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<PermissionScannerService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        return new RoleControllerFixture(
            provider,
            scope,
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<PermissionScannerService>());
    }

    private sealed class RoleControllerFixture(
        ServiceProvider provider,
        IServiceScope scope,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        PermissionScannerService permissionScanner) : IAsyncDisposable
    {
        public RoleManager<IdentityRole> RoleManager { get; } = roleManager;
        public UserManager<IdentityUser> UserManager { get; } = userManager;
        public RoleController Controller { get; } = new(roleManager, userManager, permissionScanner);

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
