using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using MvcMovie.Controllers;

namespace MvcMovie.Tests;

public class AuthorizationMetadataTests
{
    [Theory]
    [InlineData(typeof(StudentController), "CreateAjax")]
    [InlineData(typeof(StudentController), "EditAjax")]
    [InlineData(typeof(StudentController), "DeleteAjax")]
    [InlineData(typeof(StudentController), "Create")]
    [InlineData(typeof(StudentController), "Edit")]
    [InlineData(typeof(StudentController), "Delete")]
    [InlineData(typeof(StudentController), "DeleteConfirmed")]
    [InlineData(typeof(PersonController), "CreateAjax")]
    [InlineData(typeof(PersonController), "EditAjax")]
    [InlineData(typeof(PersonController), "DeleteAjax")]
    [InlineData(typeof(PersonController), "Create")]
    [InlineData(typeof(PersonController), "Edit")]
    [InlineData(typeof(PersonController), "Delete")]
    [InlineData(typeof(PersonController), "DeleteConfirmed")]
    [InlineData(typeof(MovieController), "CreateAjax")]
    [InlineData(typeof(MovieController), "EditAjax")]
    [InlineData(typeof(MovieController), "DeleteAjax")]
    public void Mutations_require_admin_role(Type controllerType, string methodName)
    {
        var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(candidate => candidate.Name == methodName)
            .ToArray();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            var authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal("Admin", authorize.Roles);
        }
    }

    [Theory]
    [InlineData(typeof(StudentController), "Index")]
    [InlineData(typeof(StudentController), "GetStudents")]
    [InlineData(typeof(StudentController), "GetStudentById")]
    [InlineData(typeof(PersonController), "Index")]
    [InlineData(typeof(PersonController), "GetPersons")]
    [InlineData(typeof(PersonController), "GetPersonById")]
    [InlineData(typeof(MovieController), "Index")]
    [InlineData(typeof(MovieController), "GetMovies")]
    [InlineData(typeof(MovieController), "GetMovieById")]
    public void Public_read_actions_do_not_require_authorization(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.Empty(method!.GetCustomAttributes<AuthorizeAttribute>());
    }
}
