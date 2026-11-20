using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Controllers;

public class MovieController : Controller
{
    private readonly ApplicationDbContext _context;

    public MovieController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        string? searchString = null,
        string? genre = null,
        int pageIndex = 1,
        int pageSize = 10)
    {
        var query = _context.Movie
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var pattern = $"%{searchString.Trim()}%";
            query = query.Where(movie => EF.Functions.Like(movie.Title!, pattern));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genrePattern = $"%{genre.Trim()}%";
            query = query.Where(movie => EF.Functions.Like(movie.Genre!, genrePattern));
        }

        var movies = query
            .OrderBy(movie => movie.Id)
            .Select(movie => new MovieResponseDto(
                movie.Id,
                movie.Title,
                movie.ReleaseDate,
                movie.Genre,
                movie.Price));

        return Ok(await PagedResult<MovieResponseDto>.CreateAsync(
            movies, pageIndex, pageSize));
    }

    [HttpGet]
    public async Task<IActionResult> GetMovieById(int id)
    {
        var movie = await _context.Movie.FindAsync(id);
        return movie == null
            ? NotFound(new { message = "Không tìm thấy phim." })
            : Ok(movie);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAjax([FromBody] Movie? movie)
    {
        if (movie == null)
        {
            return BadRequest(new { message = "Dữ liệu phim không hợp lệ." });
        }

        movie.Id = 0;
        _context.Movie.Add(movie);
        await _context.SaveChangesAsync();
        return Ok(movie);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditAjax(int id, [FromBody] Movie? movie)
    {
        if (movie == null)
        {
            return BadRequest(new { message = "Dữ liệu phim không hợp lệ." });
        }

        var existingMovie = await _context.Movie.FindAsync(id);
        if (existingMovie == null)
        {
            return NotFound(new { message = "Không tìm thấy phim." });
        }

        existingMovie.Title = movie.Title;
        existingMovie.ReleaseDate = movie.ReleaseDate;
        existingMovie.Genre = movie.Genre;
        existingMovie.Price = movie.Price;
        await _context.SaveChangesAsync();
        return Ok(existingMovie);
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var movie = await _context.Movie.FindAsync(id);
        if (movie == null)
        {
            return NotFound(new { message = "Không tìm thấy phim." });
        }

        _context.Movie.Remove(movie);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa phim." });
    }
}
