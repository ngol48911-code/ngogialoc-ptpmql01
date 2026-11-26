using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Models;

namespace MvcMovie.Controllers;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public RoleController(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
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
}
