using Abstractions.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Extensions;
using Itmo.Dev.Platform.Persistence.Postgres.Extensions;
using Lab1.Infrastructure.Persistence.HostedService;
using Lab1.Infrastructure.Persistence.Model;
using Lab1.Infrastructure.Persistence.Model.Links;
using Lab1.Infrastructure.Persistence.Options;
using Lab1.Infrastructure.Persistence.Plugins;
using Lab1.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lab1.Infrastructure.Persistence;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPlatformPersistence(persistence => persistence
            .UsePostgres(postgres => postgres
                .WithConnectionOptions(static builder => builder
                    .BindConfiguration(
                    "Infrastructure:Persistence:Postgres"))
                .WithMigrationsFrom(typeof(IMigrationsAssemblyMarker).Assembly)
                .WithDataSourcePlugin<MappingPlugin>()));

        services.AddScoped<OperationParserOptions>(servicesProvider =>
        {
            IOperationLink link = new DepositParseLink()
                .AddNext(new PayInvoiceParseLink())
                .AddNext(new WithdrawParseLink())
                .AddNext(new PaymentReceivedParseLink());

            return new OperationParserOptions
                { OperationParser = link };
        });

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAdminSessionRepository, AdminSessionRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceFactory, InvoiceFactory>();

        services.AddHostedService<MigrationHostedService>();

        return services;
    }
}