namespace MvcMovie.Models;

public sealed record MovieResponseDto(
    int Id,
    string? Title,
    DateTime ReleaseDate,
    string? Genre,
    float Price);
