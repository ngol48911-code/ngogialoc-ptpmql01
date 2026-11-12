using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Tests;

public class PersonAndMovieControllerTests
{
    [Fact]
    public async Task Person_ajax_crud_creates_updates_and_deletes()
    {
        await using var context = CreateContext();
        var controller = new PersonController(context);

        var created = Assert.IsType<OkObjectResult>(await controller.CreateAjax(new Person
        {
            PersonId = "P001",
            FullName = "Nguyễn Văn A",
            Address = "Hà Nội"
        }));
        var createdPerson = Assert.IsType<PersonResponseDto>(created.Value);

        var updated = Assert.IsType<OkObjectResult>(await controller.EditAjax("P001", new Person
        {
            FullName = "Nguyễn Văn B",
            Address = "Hải Phòng"
        }));
        var updatedPerson = Assert.IsType<PersonResponseDto>(updated.Value);

        var deleted = await controller.DeleteAjax("P001");

        Assert.Equal("P001", createdPerson.PersonId);
        Assert.Equal("Nguyễn Văn B", updatedPerson.FullName);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Null(await context.Person.FindAsync("P001"));
    }

    [Fact]
    public async Task Movie_ajax_crud_creates_updates_and_deletes()
    {
        await using var context = CreateContext();
        var controller = new MovieController(context);
        var movie = new Movie
        {
            Title = "Phim thử nghiệm",
            ReleaseDate = new DateTime(2026, 1, 1),
            Genre = "Drama",
            Price = 10.5f
        };

        var created = Assert.IsType<OkObjectResult>(await controller.CreateAjax(movie));
        var createdMovie = Assert.IsType<Movie>(created.Value);

        var updated = Assert.IsType<OkObjectResult>(await controller.EditAjax(createdMovie.Id, new Movie
        {
            Title = "Phim đã sửa",
            ReleaseDate = createdMovie.ReleaseDate,
            Genre = "Action",
            Price = 20f
        }));
        var updatedMovie = Assert.IsType<Movie>(updated.Value);

        var deleted = await controller.DeleteAjax(createdMovie.Id);

        Assert.Equal("Phim đã sửa", updatedMovie.Title);
        Assert.Equal(20f, updatedMovie.Price);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Null(await context.Movie.FindAsync(createdMovie.Id));
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
