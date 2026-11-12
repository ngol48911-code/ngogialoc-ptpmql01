using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Tests;

public class PaginationControllerTests
{
    [Fact]
    public async Task Student_search_and_faculty_filter_are_applied_before_paging()
    {
        await using var context = CreateContext();
        for (var index = 1; index <= 12; index++)
        {
            context.Student.Add(new Student
            {
                StudentId = $"28210{(index % 2 == 0 ? "02" : "01")}{index:D4}",
                FullName = $"Sinh viên {index}",
                Address = index == 10 ? "Hà Nội" : "Địa chỉ khác",
                University = "HUMG",
                NamNhapHoc = "2028",
                HeDaoTao = "21",
                FacultyId = index % 2 == 0 ? "02" : "01"
            });
        }
        await context.SaveChangesAsync();
        var controller = new StudentController(context);

        var result = await controller.GetStudents("Sinh viên", "02", 1, 5);
        var page = Assert.IsType<PagedResult<StudentResponseDto>>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal(6, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(5, page.Items.Count);
        Assert.All(page.Items, student => Assert.Equal("02", student.FacultyId));
    }

    [Fact]
    public async Task Student_paging_returns_first_middle_last_and_clamped_pages()
    {
        await using var context = CreateContext();
        for (var index = 1; index <= 12; index++)
        {
            context.Student.Add(new Student
            {
                StudentId = $"282101{index:D4}",
                FullName = $"Sinh viên {index}",
                NamNhapHoc = "2028",
                HeDaoTao = "21",
                FacultyId = "01"
            });
        }
        await context.SaveChangesAsync();
        var controller = new StudentController(context);

        var first = GetStudentPage(await controller.GetStudents(pageIndex: 1, pageSize: 5));
        var middle = GetStudentPage(await controller.GetStudents(pageIndex: 2, pageSize: 5));
        var last = GetStudentPage(await controller.GetStudents(pageIndex: 3, pageSize: 5));
        var tooHigh = GetStudentPage(await controller.GetStudents(pageIndex: 99, pageSize: 5));
        var tooLow = GetStudentPage(await controller.GetStudents(pageIndex: 0, pageSize: 5));

        Assert.Equal((1, 3, 5), (first.PageIndex, first.TotalPages, first.Items.Count));
        Assert.Equal((2, 3, 5), (middle.PageIndex, middle.TotalPages, middle.Items.Count));
        Assert.Equal((3, 3, 2), (last.PageIndex, last.TotalPages, last.Items.Count));
        Assert.Equal(3, tooHigh.PageIndex);
        Assert.Equal(1, tooLow.PageIndex);
    }

    [Fact]
    public async Task Person_search_and_movie_genre_filter_return_paged_results()
    {
        await using var context = CreateContext();
        context.Person.AddRange(
            new Person { PersonId = "P001", FullName = "Nguyễn An", Address = "Hà Nội" },
            new Person { PersonId = "P002", FullName = "Trần Bình", Address = "Hải Phòng" });
        context.Movie.AddRange(
            new Movie { Title = "Hành trình xanh", Genre = "Drama", ReleaseDate = new DateTime(2026, 1, 1) },
            new Movie { Title = "Hành trình đỏ", Genre = "Action", ReleaseDate = new DateTime(2026, 1, 2) });
        await context.SaveChangesAsync();

        var personResult = await new PersonController(context).GetPersons("nguyễn", 1, 10);
        var movieResult = await new MovieController(context).GetMovies("Hành trình", "Drama", 1, 10);
        var persons = Assert.IsType<PagedResult<PersonResponseDto>>(Assert.IsType<OkObjectResult>(personResult).Value);
        var movies = Assert.IsType<PagedResult<MovieResponseDto>>(Assert.IsType<OkObjectResult>(movieResult).Value);

        Assert.Single(persons.Items);
        Assert.Equal("P001", persons.Items[0].PersonId);
        Assert.Single(movies.Items);
        Assert.Equal("Drama", movies.Items[0].Genre);
    }

    private static PagedResult<StudentResponseDto> GetStudentPage(IActionResult result)
    {
        return Assert.IsType<PagedResult<StudentResponseDto>>(Assert.IsType<OkObjectResult>(result).Value);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"MvcMovieTests-{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
