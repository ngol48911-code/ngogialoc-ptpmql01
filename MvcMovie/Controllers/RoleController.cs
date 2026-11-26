using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Models;
using MvcMovie.Services;

namespace MvcMovie.Controllers;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly PermissionScannerService _permissionScanner;

    public RoleController(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        PermissionScannerService permissionScanner)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionScanner = permissionScanner;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(
        string? searchString = null,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var roles = _roleManager.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            var normalizedSearch = _roleManager.NormalizeKey(search)
                ?? search.ToUpperInvariant();
            roles = roles.Where(role =>
                role.NormalizedName != null
                && role.NormalizedName.Contains(normalizedSearch));
        }

        var page = await PagedResult<IdentityRole>.CreateAsync(
            roles.OrderBy(role => role.Name).ThenBy(role => role.Id),
            pageIndex,
            pageSize,
            cancellationToken);

        var items = new List<RoleResponseDto>(page.Items.Count);
        foreach (var role in page.Items)
        {
            items.Add(await ToResponseAsync(role));
        }

        return Ok(new PagedResult<RoleResponseDto>
        {
            Items = items,
            TotalItems = page.TotalItems,
            PageIndex = page.PageIndex,
            PageSize = page.PageSize,
            TotalPages = page.TotalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetRoleById(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        return role == null
            ? NotFound(new { message = "Không tìm thấy nhóm quyền." })
            : Ok(await ToResponseAsync(role));
    }

    [HttpGet]
    public async Task<IActionResult> GetRolePermissions(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound(new { message = "Không tìm thấy nhóm quyền." });
        }

        var selectedPermissions = (await _roleManager.GetClaimsAsync(role))
            .Where(claim => claim.Type == PermissionScannerService.PermissionClaimType)
            .Select(claim => claim.Value);
        return Ok(new RolePermissionManageDto(
            role.Id,
            role.Name ?? string.Empty,
            _permissionScanner.GetControllerPermissions(selectedPermissions)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRolePermissions(
        [FromBody] UpdateRolePermissionsRequest? model)
    {
        if (model == null)
        {
            return BadRequest(new { message = "Dữ liệu phân quyền không hợp lệ." });
        }

        if (ModelState.IsValid)
        {
            ValidatePermissionRequest(model);
        }
        if (!ModelState.IsValid)
        {
            return PermissionValidationError();
        }

        var role = await _roleManager.FindByIdAsync(model.RoleId!);
        if (role == null)
        {
            return NotFound(new { message = "Không tìm thấy nhóm quyền." });
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Role Admin có toàn quyền mặc định và không thể chỉnh sửa quyền Action."
            });
        }

        var requestedPermissions = model.Permissions!
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var allowedPermissions = _permissionScanner
            .GetControllerPermissions()
            .SelectMany(group => group.Actions)
            .Select(action => action.ClaimValue)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var permission in requestedPermissions)
        {
            if (!allowedPermissions.Contains(permission))
            {
                ModelState.AddModelError(
                    nameof(UpdateRolePermissionsRequest.Permissions),
                    $"Quyền '{permission}' không hợp lệ.");
            }
        }
        if (!ModelState.IsValid)
        {
            return PermissionValidationError("Danh sách quyền chứa Action không hợp lệ.");
        }

        var existingPermissions = (await _roleManager.GetClaimsAsync(role))
            .Where(claim => claim.Type == PermissionScannerService.PermissionClaimType)
            .ToArray();
        foreach (var claim in existingPermissions)
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                return PermissionIdentityError(
                    removeResult,
                    "Không thể xóa các quyền Action hiện tại.");
            }
        }

        foreach (var permission in requestedPermissions)
        {
            var addResult = await _roleManager.AddClaimAsync(
                role,
                new Claim(PermissionScannerService.PermissionClaimType, permission));
            if (!addResult.Succeeded)
            {
                return PermissionIdentityError(
                    addResult,
                    "Không thể lưu các quyền Action đã chọn.");
            }
        }

        return Ok(new { message = "Đã cập nhật quyền Action cho nhóm quyền." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAjax([FromBody] RoleDto? model)
    {
        if (model == null)
        {
            return BadRequest(new { message = "Dữ liệu nhóm quyền không hợp lệ." });
        }

        var normalizedModel = model with { Name = model.Name?.Trim() };
        if (ModelState.IsValid)
        {
            ValidateModel(normalizedModel);
        }
        if (!ModelState.IsValid)
        {
            return ValidationError();
        }

        var name = normalizedModel.Name!;
        if (await _roleManager.RoleExistsAsync(name))
        {
            ModelState.AddModelError(nameof(RoleDto.Name), "Tên nhóm quyền đã tồn tại.");
            return ValidationError();
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(name));
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationError();
        }

        var role = await _roleManager.FindByNameAsync(name);
        return role == null
            ? StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Không thể đọc nhóm quyền vừa tạo." })
            : Ok(await ToResponseAsync(role));
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAjax(string id, [FromBody] RoleDto? model)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Không tìm thấy nhóm quyền." });
        }

        if (IsSystemRole(role.Name))
        {
            return BadRequest(new { message = "Không thể sửa tên nhóm quyền hệ thống Admin hoặc User." });
        }

        if (model == null)
        {
            return BadRequest(new { message = "Dữ liệu nhóm quyền không hợp lệ." });
        }

        var normalizedModel = model with { Name = model.Name?.Trim() };
        if (ModelState.IsValid)
        {
            ValidateModel(normalizedModel);
        }
        if (!ModelState.IsValid)
        {
            return ValidationError();
        }

        var name = normalizedModel.Name!;
        if (await _roleManager.RoleExistsAsync(name))
        {
            var duplicateRole = await _roleManager.FindByNameAsync(name);
            if (duplicateRole?.Id != role.Id)
            {
                ModelState.AddModelError(nameof(RoleDto.Name), "Tên nhóm quyền đã tồn tại.");
                return ValidationError();
            }
        }

        role.Name = name;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationError();
        }

        return Ok(await ToResponseAsync(role));
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Không tìm thấy nhóm quyền." });
        }

        if (IsSystemRole(role.Name))
        {
            return BadRequest(new { message = "Không thể xóa nhóm quyền hệ thống Admin hoặc User." });
        }

        var usersCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
        if (usersCount > 0)
        {
            return BadRequest(new
            {
                message = $"Không thể xóa nhóm quyền đang được gán cho {usersCount} người dùng. " +
                    "Hãy chuyển người dùng sang nhóm quyền khác trước."
            });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationError();
        }

        return Ok(new { message = "Đã xóa nhóm quyền." });
    }

    private async Task<RoleResponseDto> ToResponseAsync(IdentityRole role)
    {
        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
        return new RoleResponseDto(
            role.Id,
            role.Name ?? string.Empty,
            role.NormalizedName,
            userCount,
            IsSystemRole(role.Name));
    }

    private static bool IsSystemRole(string? name)
    {
        return string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "User", StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateModel(RoleDto model)
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
    }

    private void ValidatePermissionRequest(UpdateRolePermissionsRequest model)
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

        if (model.Permissions?.Any(string.IsNullOrWhiteSpace) == true)
        {
            ModelState.AddModelError(
                nameof(UpdateRolePermissionsRequest.Permissions),
                "Giá trị quyền không được để trống.");
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(nameof(RoleDto.Name), error.Description);
        }
    }

    private BadRequestObjectResult ValidationError()
    {
        return BadRequest(new
        {
            message = "Vui lòng kiểm tra lại dữ liệu.",
            errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToArray())
        });
    }

    private BadRequestObjectResult PermissionValidationError(
        string message = "Vui lòng kiểm tra lại danh sách quyền.")
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

    private BadRequestObjectResult PermissionIdentityError(
        IdentityResult result,
        string message)
    {
        return BadRequest(new
        {
            message,
            errors = new Dictionary<string, string[]>
            {
                [nameof(UpdateRolePermissionsRequest.Permissions)] =
                    result.Errors.Select(error => error.Description).ToArray()
            }
        });
    }
}
