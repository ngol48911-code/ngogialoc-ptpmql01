namespace MvcMovie.Models;

public sealed record PersonResponseDto(
    string? PersonId,
    string? FullName,
    string? Address);
