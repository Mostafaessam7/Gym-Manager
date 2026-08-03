using GymManager.SharedKernel.Cqrs;
using NetArchTest.Rules;
using Xunit;

namespace GymManager.ArchitectureTests;

public sealed class ConventionTests
{
    [Fact]
    public void Command_Handlers_Should_Be_Sealed()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Query_Handlers_Should_Be_Sealed()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Command_Handler_Names_Should_End_With_CommandHandler()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Query_Handler_Names_Should_End_With_QueryHandler()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_EntityFrameworkCore()
    {
        var result = Types.InAssembly(typeof(Domain.Abstractions.IUnitOfWork).Assembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Repository_Implementations_Should_Be_Internal()
    {
        var result = Types.InAssembly(typeof(Infrastructure.DependencyInjection).Assembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
