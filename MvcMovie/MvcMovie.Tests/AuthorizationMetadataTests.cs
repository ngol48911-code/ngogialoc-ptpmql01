using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using MvcMovie.Controllers;

namespace MvcMovie.Tests;

public class AuthorizationMetadataTests
{
    [Theory]
    [InlineData(typeof(StudentController))]
    [InlineData(typeof(PersonController))]
    [InlineData(typeof(MovieController))]
    public void Data_controllers_require_login_and_delegate_action_permissions(Type controllerType)
    {
        var authorize = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Null(authorize.Roles);

        var methods = controllerType.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.All(methods, method =>
            Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>()));
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
