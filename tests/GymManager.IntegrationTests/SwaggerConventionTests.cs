using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Confirms <c>ConventionalResponsesOperationFilter</c> (see <c>GymManager.Api/Configuration</c>) actually
/// enriches the generated OpenAPI document with 401/403/404/400 responses, rather than every action falling
/// back to Swashbuckle's un-enriched single-200-response default — the exact gap PROJECT_STATUS.md flagged
/// ("zero controllers carry `[ProducesResponseType]`"). An earlier attempt using ASP.NET Core's built-in
/// <c>[ApiConventionType(typeof(DefaultApiConventions))]</c> was tried first and this same test caught that
/// it had no effect, which is why a hand-written filter is used instead. `AddSwaggerGen` is registered
/// unconditionally in <c>ServiceCollectionExtensions</c>, so <see cref="ISwaggerProvider"/> is resolvable
/// even in the "Testing" environment, where the <c>UseSwagger()</c> middleware itself is disabled.
/// </summary>
public sealed class SwaggerConventionTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void Swagger_Document_Should_Document_401_403_For_Authorized_Endpoints()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        // GetMemberById requires [Authorize] + a permission — not anonymous.
        var getMemberById = document.Paths["/api/v1/members/{id}"].Operations[OperationType.Get];
        Assert.Contains("401", getMemberById.Responses.Keys);
        Assert.Contains("403", getMemberById.Responses.Keys);
    }

    [Fact]
    public void Swagger_Document_Should_Not_Document_401_For_Anonymous_Auth_Endpoints()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        // Login is [AllowAnonymous] — it must never claim to require a bearer token to reach it.
        var login = document.Paths["/api/v1/auth/login"].Operations[OperationType.Post];
        Assert.DoesNotContain("401", login.Responses.Keys);
    }

    [Fact]
    public void Swagger_Document_Should_Document_404_For_Id_Routed_Gets_Puts_And_Deletes()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        var getMemberById = document.Paths["/api/v1/members/{id}"].Operations[OperationType.Get];
        Assert.Contains("404", getMemberById.Responses.Keys);

        var deleteMember = document.Paths["/api/v1/members/{id}"].Operations[OperationType.Delete];
        Assert.Contains("404", deleteMember.Responses.Keys);
    }

    [Fact]
    public void Swagger_Document_Should_Document_400_For_Post_And_Put_Endpoints()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        var createMember = document.Paths["/api/v1/members"].Operations[OperationType.Post];
        Assert.Contains("400", createMember.Responses.Keys);

        var updateMember = document.Paths["/api/v1/members/{id}"].Operations[OperationType.Put];
        Assert.Contains("400", updateMember.Responses.Keys);
    }
}
