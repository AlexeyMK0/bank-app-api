using Itmo.Dev.Platform.Testing.ApplicationFactories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace IntegrationalTests.Fixtures;

public class WebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:latest").Build();

#pragma warning disable SK1200
    private WebApplicationFactory<Program> _webApplicationFactory = null!;
#pragma warning restore SK1200

    public IServiceProvider Services => _webApplicationFactory.Services;

    public async Task InitializeAsync()
    {
        // TODO: нужен ли cancellationToken
        var ctSource = new CancellationTokenSource();
        try
        {
            ctSource.CancelAfter(TimeSpan.FromMinutes(1));
            await _container.StartAsync(ctSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            ctSource.Dispose();
        }

        _webApplicationFactory = new PlatformWebApplicationBuilder<Program>()
            .ConfigureConfiguration(builder => builder.AddInMemoryCollection(new Dictionary<string, string?>
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
            }))
            .Build();

        _webApplicationFactory.StartServer();
    }

    public async Task DisposeAsync()
    {
        await _webApplicationFactory.DisposeAsync();
        await _container.DisposeAsync();
    }
}