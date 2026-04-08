#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1775124350, "Invoices")]
public class AddedInvoices : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return """
        CREATE TYPE INVOICE_STATUS AS ENUM ('created', 'paid', 'cancelled');        

        CREATE TABLE invoices
        (
           invoice_id BIGSERIAL PRIMARY KEY,
           state INVOICE_STATUS NOT NULL,
           amount decimal NOT NULL,
           recipient_id BIGINT NOT NULL,
           payer_id BIGINT NOT NULL
        )
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """
        DROP TABLE IF EXISTS invoices;

        DROP TYPE IF EXISTS INVOICE_STATUS;
        """;
    }
}