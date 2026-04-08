using Itmo.Dev.Platform.Persistence.Postgres.Plugins;
using Lab1.Domain.Invoices;
using Npgsql;

namespace Lab1.Infrastructure.Persistence.Plugins;

public class MappingPlugin : IPostgresDataSourcePlugin
{
    public void Configure(NpgsqlDataSourceBuilder dataSource)
    {
        dataSource.MapEnum<InvoiceStatus>();
    }
}