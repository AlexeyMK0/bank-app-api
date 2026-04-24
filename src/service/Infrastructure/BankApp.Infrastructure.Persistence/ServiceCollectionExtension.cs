using BankApp.Application.Abstractions.Repositories;
using BankApp.Infrastructure.Persistence.Model;
using BankApp.Infrastructure.Persistence.Model.Links;
using BankApp.Infrastructure.Persistence.Options;
using BankApp.Infrastructure.Persistence.Plugins;
using BankApp.Infrastructure.Persistence.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Extensions;
using Itmo.Dev.Platform.Persistence.Postgres.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Infrastructure.Persistence;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IInvoiceFactory, InvoiceFactory>();

        return services;
    }
}