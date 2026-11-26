namespace MvcMovie.Models;

public sealed record RoleResponseDto(
    string Id,
    string Name,
    string? NormalizedName,
    int UserCount,
    bool IsSystemRole);
