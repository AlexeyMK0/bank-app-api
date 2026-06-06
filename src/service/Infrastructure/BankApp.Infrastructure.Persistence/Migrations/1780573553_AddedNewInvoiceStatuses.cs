#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1780573553, "AddedNewInvoiceStatuses")]
public class AddedNewInvoiceStatuses : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return
        """
        ALTER TYPE invoice_status ADD VALUE IF NOT EXISTS 'approved'; 
        ALTER TYPE invoice_status ADD VALUE IF NOT EXISTS 'declined'; 
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        UPDATE invoices
        SET state = 'created'
        WHERE state in ('approved', 'declined');

        ALTER TYPE invoice_status RENAME TO invoice_status_old;

        CREATE TYPE invoice_status AS ENUM ('created', 'paid', 'cancelled');

        ALTER TABLE invoices 
            ALTER COLUMN state TYPE invoice_status 
            USING state::text::invoice_status;

        DROP TYPE invoice_status_old;
        """;
    }
}