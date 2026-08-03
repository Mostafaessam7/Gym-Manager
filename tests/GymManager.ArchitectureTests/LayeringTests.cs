using NetArchTest.Rules;
using Xunit;

namespace GymManager.ArchitectureTests;

public sealed class LayeringTests
{
    private const string DomainNamespace = "GymManager.Domain";
    private const string ApplicationNamespace = "GymManager.Application";
    private const string InfrastructureNamespace = "GymManager.Infrastructure";
    private const string ApiNamespace = "GymManager.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(Domain.Abstractions.IUnitOfWork).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(Infrastructure.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
