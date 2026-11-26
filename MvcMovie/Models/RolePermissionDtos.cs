using System.ComponentModel.DataAnnotations;

namespace MvcMovie.Models;

public sealed record ActionPermissionItem(
    string ActionName,
    string ClaimValue,
    bool IsSelected);

public sealed record ControllerPermissionGroup(
    string ControllerName,
    IReadOnlyList<ActionPermissionItem> Actions);

public sealed record RolePermissionManageDto(
    string RoleId,
    string RoleName,
    IReadOnlyList<ControllerPermissionGroup> Controllers);

public sealed record UpdateRolePermissionsRequest
{
    [Required(ErrorMessage = "Mã nhóm quyền là bắt buộc.")]
    public string? RoleId { get; init; }

    [Required(ErrorMessage = "Danh sách quyền là bắt buộc.")]
    public string[]? Permissions { get; init; }
}
