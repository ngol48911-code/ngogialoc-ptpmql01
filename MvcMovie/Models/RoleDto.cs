using System.ComponentModel.DataAnnotations;

namespace MvcMovie.Models;

public sealed record RoleDto
{
    public string? Id { get; init; }

    [Required(ErrorMessage = "Tên nhóm quyền là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Tên nhóm quyền không được vượt quá {1} ký tự.")]
    public string? Name { get; init; }
}
