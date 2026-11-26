using System.ComponentModel.DataAnnotations;

namespace MvcMovie.Models;

public sealed record UserResponseDto(
    string Id,
    string? Email,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles);

public sealed record RoleSelectionItem(
    string Name,
    bool IsSelected);

public sealed record UserRoleManageDto(
    string UserId,
    string? Email,
    IReadOnlyList<RoleSelectionItem> Roles);

public sealed record UpdateUserRolesRequest
{
    [Required(ErrorMessage = "Mã người dùng là bắt buộc.")]
    public string? UserId { get; init; }

    [Required(ErrorMessage = "Danh sách nhóm quyền là bắt buộc.")]
    public string[]? Roles { get; init; }
}
