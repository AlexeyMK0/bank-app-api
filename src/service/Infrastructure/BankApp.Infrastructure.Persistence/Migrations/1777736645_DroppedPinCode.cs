#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1777736645, "DroppedPinCode")]
public class DroppedPinCode : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts DROP COLUMN account_pincode;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ADD COLUMN account_pincode text;
        """;
    }
}