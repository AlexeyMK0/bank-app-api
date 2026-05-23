#pragma warning disable SK1200

using FluentMigrator.Runner;
using Grpc.Net.Client;
using Itmo.Dev.Platform.Testing.ApplicationFactories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using System.Data;
using System.Data.Common;
using Testcontainers.PostgreSql;

namespace IntegrationalTests.Fixtures;

public class WebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:latest")
        .WithReuse(true)
        .Build();

    private Respawner? _respawner = null;

    private WebApplicationFactory<Program> _webApplicationFactory = null!;

    public IServiceProvider Services => _webApplicationFactory.Services;

    private static readonly string[] RespawnSchemasToInclude = new[] { "public" };

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _webApplicationFactory = new PlatformWebApplicationBuilder<Program>()
            .ConfigureConfiguration(ConfigureAppConfiguration)
            .Build();

        using (IServiceScope scope = _webApplicationFactory.Services.CreateScope())
        {
            IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        _webApplicationFactory.StartServer();
    }

    public async Task DisposeAsync()
    {
        await _webApplicationFactory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        Respawner respawner = await GetRespawner();

        await using DbConnection conn = GetConnection();
        if (conn.State is not ConnectionState.Open)
            await conn.OpenAsync();

        await respawner.ResetAsync(conn);
    }

    public GrpcChannel CreateChannel()
    {
        var grpcChannelOptions = new GrpcChannelOptions
        {
            HttpHandler = _webApplicationFactory.Server.CreateHandler(),
        };

        return GrpcChannel.ForAddress("http://localhost/", grpcChannelOptions);
    }

    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_container.GetConnectionString());
    }

    private void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Infrastructure:Persistence:Postgres:Host", _container.Hostname },
            {
                "Infrastructure:Persistence:Postgres:Port",
                _container.GetMappedPublicPort(5432).ToString()
            },
            { "Infrastructure:Persistence:Postgres:Database", "postgres" },
            { "Infrastructure:Persistence:Postgres:Username", "postgres" },
            { "Infrastructure:Persistence:Postgres:Password", "postgres" },
            { "Infrastructure:Persistence:Postgres:SslMode", "Prefer" },
            { "Services:Accounts:MaxAccountsPerUser", "3" },
        });
    }

    private async ValueTask<Respawner> GetRespawner()
    {
        if (_respawner is not null)
            return _respawner;

        await using DbConnection connection = GetConnection();

        if (connection.State is not ConnectionState.Open)
            await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions()
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = RespawnSchemasToInclude,
                TablesToIgnore = ["VersionInfo"],
            });

        return _respawner;
    }
}