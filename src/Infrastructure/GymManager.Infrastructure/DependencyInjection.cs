using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Attendance;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.Branches;
using GymManager.Domain.Classes;
using GymManager.Domain.Crm;
using GymManager.Domain.Expenses;
using GymManager.Domain.GiftCards;
using GymManager.Domain.Identity;
using GymManager.Domain.Invoices;
using GymManager.Domain.Lockers;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Notifications;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Payments;
using GymManager.Domain.Products;
using GymManager.Domain.Sales;
using GymManager.Domain.Settings;
using GymManager.Domain.Staff;
using GymManager.Domain.Trainers;
using GymManager.Domain.Workouts;
using GymManager.Infrastructure.Attendance;
using GymManager.Infrastructure.Authentication;
using GymManager.Infrastructure.Caching;
using GymManager.Infrastructure.Events;
using GymManager.Infrastructure.Files;
using GymManager.Infrastructure.Notifications;
using GymManager.Infrastructure.PaymentGateways;
using GymManager.Infrastructure.Persistence;
using GymManager.Infrastructure.Persistence.Repositories;
using GymManager.Infrastructure.Web;
using GymManager.Infrastructure.Reports;
using GymManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GymManager.Infrastructure;

/// <summary>Composition root for the infrastructure layer: persistence, identity services and cross-cutting adapters.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GymManagerDatabase")
            ?? throw new InvalidOperationException("Connection string 'GymManagerDatabase' was not found.");

        services.AddDbContext<GymManagerDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.MigrationsAssembly(typeof(GymManagerDbContext).Assembly.FullName);
            }));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GymManagerDbContext>());
        services.AddScoped<IApplicationReadDb>(sp => sp.GetRequiredService<GymManagerDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITwoFactorService>(_ => new TotpTwoFactorService(jwtOptions.Issuer));

        // Every configured gateway is registered as its own IPaymentGatewayService instance; the application
        // layer's PaymentGatewayServiceResolver picks the right one per-call by PaymentGatewayProvider (see
        // its remarks for why a single, directly-injected IPaymentGatewayService no longer works now that more
        // than one provider can be registered at once).
        var stripeOptions = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>()
            ?? throw new InvalidOperationException("Stripe configuration section is missing.");
        services.AddSingleton(stripeOptions);
        services.AddSingleton<IPaymentGatewayService, StripePaymentGatewayService>();

        var paymobOptions = configuration.GetSection(PaymobOptions.SectionName).Get<PaymobOptions>()
            ?? throw new InvalidOperationException("Paymob configuration section is missing.");
        services.AddSingleton(paymobOptions);
        services.AddSingleton<IPaymentGatewayService, PaymobPaymentGatewayService>();

        var fawryOptions = configuration.GetSection(FawryOptions.SectionName).Get<FawryOptions>()
            ?? throw new InvalidOperationException("Fawry configuration section is missing.");
        services.AddSingleton(fawryOptions);
        services.AddSingleton<IPaymentGatewayService, FawryPaymentGatewayService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IBodyMeasurementRepository, BodyMeasurementRepository>();
        services.AddScoped<IWorkoutPlanRepository, WorkoutPlanRepository>();
        services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
        services.AddScoped<INutritionPlanRepository, NutritionPlanRepository>();
        services.AddScoped<INutritionLogRepository, NutritionLogRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IGiftCardRepository, GiftCardRepository>();
        services.AddScoped<IStaffShiftRepository, StaffShiftRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddSingleton<IBarcodeGenerator, BarcodeGenerator>();
        services.AddScoped<ITrainerRepository, TrainerRepository>();
        services.AddScoped<IGymClassRepository, GymClassRepository>();
        services.AddScoped<IClassSessionRepository, ClassSessionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ILockerRepository, LockerRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
            ?? throw new InvalidOperationException("Email configuration section is missing.");
        services.AddSingleton(emailOptions);
        services.AddSingleton<IEmailSender, EmailSender>();

        // Unlike the payment gateways above, Twilio configuration is optional: SMS is an auxiliary
        // notification channel (reminders, expiry nudges), not a flow the app depends on to function, so a
        // deployment that never configures it should still start and work — falling back to
        // LoggingSmsSender, exactly the zero-config behavior this app always had before Twilio was wired in.
        var twilioOptions = configuration.GetSection(TwilioOptions.SectionName).Get<TwilioOptions>();
        var twilioConfigured = twilioOptions is { AccountSid.Length: > 0, AuthToken.Length: > 0, FromPhoneNumber.Length: > 0 };
        if (twilioConfigured)
        {
            services.AddSingleton(twilioOptions!);
            services.AddSingleton<ISmsSender, TwilioSmsSender>();
        }
        else
        {
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
        }

        services.AddSingleton<IReportExporter, ReportExporter>();

        // Cache backing store. Redis when ConnectionStrings:Redis is set, per-process memory
        // otherwise.
        //
        // The memory fallback is deliberate: requiring Redis unconditionally would mean local
        // development, CI and the whole test suite need a Redis server running, and the realistic
        // outcome of that is someone disabling caching instead. Both sit behind ICacheService, so
        // no caller changes - only the store does.
        //
        // They are NOT equivalent once more than one instance runs, which is the reason to
        // configure Redis. With per-process memory, a branch or plan edit invalidates the cache on
        // the instance that handled the write and leaves the others serving stale data until
        // expiry. Nothing errors - answers just differ by which instance replied.
        services.AddMemoryCache();

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnectionString));

            // Keyspace prefix so several apps can share one Redis without colliding.
            services.AddSingleton<ICacheService>(
                sp => new RedisCacheService(sp.GetRequiredService<IConnectionMultiplexer>(), "gymmanager:"));
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        var fileStorageOptions = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
            ?? throw new InvalidOperationException("FileStorage configuration section is missing.");
        services.AddSingleton(fileStorageOptions);
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        var clientOptions = configuration.GetSection(ClientOptions.SectionName).Get<ClientOptions>() ?? new ClientOptions();
        services.AddSingleton(clientOptions);
        services.AddSingleton<IClientUrlProvider, ClientUrlProvider>();

        return services;
    }
}
