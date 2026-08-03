using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Verifies the localization infrastructure PROJECT_STATUS.md flagged as "plumbing only" now actually
/// serves translated content: <c>Accept-Language: es-ES</c> gets a Spanish error message where a resource
/// entry exists in <c>Resources/ErrorMessages.es.resx</c>, and an unset/unsupported language keeps the
/// original English <c>Error.Message</c> (proving the fallback path — most of the ~150 error codes have no
/// translated entry yet, which is expected).
/// </summary>
public sealed class LocalizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task NotFound_Error_With_Spanish_AcceptLanguage_Should_Return_Translated_Detail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("es-ES"));

        var response = await client.GetAsync($"/api/v1/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("No se encontró el socio.", problem!.Detail);
    }

    [Fact]
    public async Task NotFound_Error_With_Arabic_AcceptLanguage_Should_Return_Translated_Detail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar-SA"));

        var response = await client.GetAsync($"/api/v1/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("لم يتم العثور على العضو.", problem!.Detail);
    }

    [Fact]
    public async Task NotFound_Error_Without_AcceptLanguage_Should_Return_English_Detail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync($"/api/v1/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("The member was not found.", problem!.Detail);
    }
}
