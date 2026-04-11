using BankApp.Domain.Invoices;
using Itmo.Dev.Platform.Persistence.Postgres.Plugins;
using Npgsql;

namespace BankApp.Infrastructure.Persistence.Plugins;

public class MappingPlugin : IPostgresDataSourcePlugin
{
    public void Configure(NpgsqlDataSourceBuilder dataSource)
    {
        dataSource.MapEnum<InvoiceStatus>();
    }
}