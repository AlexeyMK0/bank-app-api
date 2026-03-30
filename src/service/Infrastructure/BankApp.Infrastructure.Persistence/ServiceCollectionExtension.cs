using Abstractions.Repositories;
using Abstractions.Transactions;
using FluentMigrator.Runner;
using Lab1.Domain.Operations;
using Lab1.Infrastructure.Persistence.Connections;
using Lab1.Infrastructure.Persistence.HostedService;
using Lab1.Infrastructure.Persistence.PersistenceEntities;
using Lab1.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lab1.Infrastructure.Persistence;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAdminSessionRepository, AdminSessionRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();

        services.AddScoped<PostgresDbSession>();
        services.AddScoped<IConnectionProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<PostgresDbSession>());
        services.AddScoped<ITransactionProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<PostgresDbSession>());

        services.AddOptions<ConnectionOptions>()
            .BindConfiguration("Infrastructure:Persistence:Postgres")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
        {
            ConnectionOptions options = serviceProvider.GetRequiredService<IOptions<ConnectionOptions>>().Value;
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);
            dataSourceBuilder.MapEnum<OperationType>();
            return dataSourceBuilder.Build();
        });

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(serviceProvider =>
                {
                    ConnectionOptions options = serviceProvider.GetRequiredService<IOptions<ConnectionOptions>>().Value;
                    return options.ConnectionString;
                })
                .WithMigrationsIn(typeof(IMigrationsAssemblyMarker).Assembly));

        services.AddHostedService<MigrationHostedService>();

        return services;
    }
}