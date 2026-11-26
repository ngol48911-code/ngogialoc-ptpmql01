using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Models;

namespace MvcMovie.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.RoleFilters = await _roleManager.Roles
            .AsNoTracking()
            .Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        string? searchString = null,
        string? roleFilter = null,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = _userManager.NormalizeEmail(searchString.Trim())
                ?? searchString.Trim().ToUpperInvariant();
            users = users.Where(user =>
                user.NormalizedEmail != null
                && user.NormalizedEmail.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var role = await _roleManager.FindByNameAsync(roleFilter.Trim());
            if (role == null)
            {
                users = users.Where(_ => false);
            }
            else
            {
                var matchingUsers = await _userManager.GetUsersInRoleAsync(role.Name!);
                var userIds = matchingUsers.Select(user => user.Id).ToArray();
                users = users.Where(user => userIds.Contains(user.Id));
            }
        }

        var page = await PagedResult<IdentityUser>.CreateAsync(
            users.OrderBy(user => user.Email).ThenBy(user => user.Id),
            pageIndex,
            pageSize,
            cancellationToken);

        var items = new List<UserResponseDto>(page.Items.Count);
        foreach (var user in page.Items)
        {
            items.Add(new UserResponseDto(
                user.Id,
                user.Email,
                user.EmailConfirmed,
                (await _userManager.GetRolesAsync(user)).ToArray()));
        }

        return Ok(new PagedResult<UserResponseDto>
        {
            Items = items,
            TotalItems = page.TotalItems,
            PageIndex = page.PageIndex,
            PageSize = page.PageSize,
            TotalPages = page.TotalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        return Ok(await ToManageDtoAsync(user));
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest? model)
    {
        if (!Request.Headers.ContainsKey("RequestVerificationToken"))
        {
            return BadRequest(new
            {
                message = "Thiếu mã xác thực RequestVerificationToken."
            });
        }

        if (model == null)
        {
            return BadRequest(new { message = "Dữ liệu phân quyền không hợp lệ." });
        }

        if (ModelState.IsValid)
        {
            ValidateRequest(model);
        }
        if (!ModelState.IsValid)
        {
            return ValidationError();
        }

        var user = await _userManager.FindByIdAsync(model.UserId!);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var requestedRoles = model.Roles!
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Admin")
            && string.Equals(currentUserId, user.Id, StringComparison.Ordinal)
            && !requestedRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Bạn không thể tự gỡ quyền Admin của chính mình."
            });
        }

        foreach (var roleName in requestedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError(
                    nameof(UpdateUserRolesRequest.Roles),
                    $"Nhóm quyền '{roleName}' không tồn tại.");
            }
        }
        if (!ModelState.IsValid)
        {
            return ValidationError("Một hoặc nhiều nhóm quyền không hợp lệ.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = requestedRoles
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolesToRemove = currentRoles
            .Except(requestedRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return IdentityError(addResult, "Không thể gán nhóm quyền cho người dùng.");
            }
        }

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return IdentityError(removeResult, "Không thể gỡ nhóm quyền khỏi người dùng.");
            }
        }

        return Ok(await ToManageDtoAsync(user));
    }

    private async Task<UserRoleManageDto> ToManageDtoAsync(IdentityUser user)
    {
        var selectedRoles = await _userManager.GetRolesAsync(user);
        var selectedRoleNames = new HashSet<string>(
            selectedRoles,
            StringComparer.OrdinalIgnoreCase);
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();

        return new UserRoleManageDto(
            user.Id,
            user.Email,
            roles.Select(name => new RoleSelectionItem(
                name,
                selectedRoleNames.Contains(name))).ToArray());
    }

    private void ValidateRequest(UpdateUserRolesRequest model)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var memberName = validationResult.MemberNames.FirstOrDefault();
            ModelState.AddModelError(
                memberName ?? string.Empty,
                validationResult.ErrorMessage ?? "Dữ liệu không hợp lệ.");
        }

        if (model.Roles?.Any(string.IsNullOrWhiteSpace) == true)
        {
            ModelState.AddModelError(
                nameof(UpdateUserRolesRequest.Roles),
                "Tên nhóm quyền không được để trống.");
        }
    }

    private BadRequestObjectResult IdentityError(IdentityResult result, string message)
    {
        return BadRequest(new
        {
            message,
            errors = new Dictionary<string, string[]>
            {
                [nameof(UpdateUserRolesRequest.Roles)] =
                    result.Errors.Select(error => error.Description).ToArray()
            }
        });
    }

    private BadRequestObjectResult ValidationError(
        string message = "Vui lòng kiểm tra lại dữ liệu.")
    {
        return BadRequest(new
        {
            message,
            errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .Where(error => !string.IsNullOrWhiteSpace(error))
                        .ToArray())
        });
    }
}
