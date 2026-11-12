using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Controllers;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Tests;

public class StudentControllerTests
{
    [Fact]
    public async Task CreateAjax_generates_first_and_next_student_ids()
    {
        await using var context = CreateContext();
        var controller = new StudentController(context);

        var firstResult = await controller.CreateAjax(CreateStudent());
        var secondResult = await controller.CreateAjax(CreateStudent());

        var first = Assert.IsType<StudentResponseDto>(Assert.IsType<OkObjectResult>(firstResult).Value);
        var second = Assert.IsType<StudentResponseDto>(Assert.IsType<OkObjectResult>(secondResult).Value);

        Assert.Equal("2821010001", first.StudentId);
        Assert.Equal("2821010002", second.StudentId);
    }

    [Fact]
    public async Task CreateAjax_returns_bad_request_when_required_fields_are_missing()
    {
        await using var context = CreateContext();
        var controller = new StudentController(context);

        var result = await controller.CreateAjax(new Student
        {
            NamNhapHoc = "2028",
            HeDaoTao = "21"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = JsonSerializer.Serialize(badRequest.Value);

        Assert.Contains("FullName", payload);
        Assert.Contains("FacultyId", payload);
    }

    [Fact]
    public async Task GetStudents_returns_faculty_name_without_navigation_loop()
    {
        await using var context = CreateContext();
        context.Student.Add(CreateStudent("2821010001"));
        await context.SaveChangesAsync();
        var controller = new StudentController(context);

        var result = await controller.GetStudents();

        var students = Assert.IsType<PagedResult<StudentResponseDto>>(Assert.IsType<OkObjectResult>(result).Value);
        var student = Assert.Single(students.Items);
        Assert.Equal("Công nghệ thông tin", student.FacultyName);
        Assert.DoesNotContain("\"Faculty\":", JsonSerializer.Serialize(student));
    }

    [Fact]
    public async Task EditAjax_updates_data_and_preserves_student_id()
    {
        await using var context = CreateContext();
        context.Student.Add(CreateStudent("2821010001"));
        await context.SaveChangesAsync();
        var controller = new StudentController(context);

        var result = await controller.EditAjax("2821010001", new Student
        {
            StudentId = "tam-thoi",
            FullName = "Sinh viên đã sửa",
            Address = "Hà Nội mới",
            University = "Trường Đại học Mỏ - Địa chất",
            NamNhapHoc = "2028",
            HeDaoTao = "21",
            FacultyId = "01"
        });

        var updated = Assert.IsType<StudentResponseDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("2821010001", updated.StudentId);
        Assert.Equal("Sinh viên đã sửa", updated.FullName);
        Assert.Equal("Hà Nội mới", updated.Address);
    }

    [Fact]
    public async Task DeleteAjax_deletes_existing_student_and_returns_not_found_for_unknown_id()
    {
        await using var context = CreateContext();
        context.Student.Add(CreateStudent("2821010001"));
        await context.SaveChangesAsync();
        var controller = new StudentController(context);

        var deletedResult = await controller.DeleteAjax("2821010001");
        var missingResult = await controller.DeleteAjax("not-found");

        Assert.IsType<OkObjectResult>(deletedResult);
        Assert.IsType<NotFoundObjectResult>(missingResult);
        Assert.Null(await context.Student.FindAsync("2821010001"));
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

    private static Student CreateStudent(string? studentId = null)
    {
        return new Student
        {
            StudentId = studentId,
            FullName = "Nguyễn Văn A",
            Address = "Hà Nội",
            University = "Trường Đại học Mỏ - Địa chất",
            NamNhapHoc = "2028",
            HeDaoTao = "21",
            FacultyId = "01"
        };
    }
}
