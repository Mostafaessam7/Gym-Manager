using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GymManager.IntegrationTests.Fakes;

/// <summary>
/// Same InMemory-database swap as <see cref="CustomWebApplicationFactory"/>, plus replacing the real
/// gateway-backed <see cref="IPaymentGatewayService"/> registrations with two <see cref="FakePaymentGatewayService"/>
/// instances (one per <see cref="Gateway"/>/<see cref="PaymobGateway"/>) the test can control and inspect —
/// kept as its own factory rather than added to <see cref="CustomWebApplicationFactory"/> so every other
/// test's DI container is untouched. Registering two, not one, is what actually proves
/// <c>PaymentGatewayServiceResolver</c> picks the right gateway per request rather than always resolving
/// whichever was registered first/last.
/// </summary>
public sealed class PaymentGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"GymManagerTests-{Guid.NewGuid()}";

    public FakePaymentGatewayService Gateway { get; } = new() { Provider = PaymentGatewayProvider.Stripe };

    public FakePaymentGatewayService PaymobGateway { get; } = new() { Provider = PaymentGatewayProvider.Paymob };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var contextDescriptors = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(GymManagerDbContext)))
                .ToList();
            foreach (var contextDescriptor in contextDescriptors)
                services.Remove(contextDescriptor);

            services.AddDbContext<GymManagerDbContext>(options => options.UseInMemoryDatabase(DatabaseName));

            services.RemoveAll<IPaymentGatewayService>();
            services.AddSingleton<IPaymentGatewayService>(Gateway);
            services.AddSingleton<IPaymentGatewayService>(PaymobGateway);

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}
