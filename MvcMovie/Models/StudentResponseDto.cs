namespace MvcMovie.Models;

public sealed record StudentResponseDto(
    string? StudentId,
    string? FullName,
    string? Address,
    string? University,
    string? NamNhapHoc,
    string? HeDaoTao,
    string? FacultyId,
    string? FacultyName);
