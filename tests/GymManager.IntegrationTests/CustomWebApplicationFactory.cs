using GymManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManager.IntegrationTests;

/// <summary>
/// Swaps the SQL Server-backed <see cref="GymManagerDbContext"/> for an isolated EF Core InMemory database
/// per factory instance. This sandbox has no SQL Server/Docker available, so InMemory is used to exercise
/// the full request pipeline (routing, auth, validation, dispatcher, persistence) end-to-end without one.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"GymManagerTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext registers more than just DbContextOptions<T> — later EF Core versions also
            // register a per-context "options configuration" descriptor that gets applied when the
            // options are built. Leaving that in place alongside the InMemory registration would
            // configure both SqlServer and InMemory providers on the same context, so every descriptor
            // closed over GymManagerDbContext is removed before re-registering it.
            var contextDescriptors = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(GymManagerDbContext)))
                .ToList();
            foreach (var contextDescriptor in contextDescriptors)
                services.Remove(contextDescriptor);

            services.AddDbContext<GymManagerDbContext>(options => options.UseInMemoryDatabase(DatabaseName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}
