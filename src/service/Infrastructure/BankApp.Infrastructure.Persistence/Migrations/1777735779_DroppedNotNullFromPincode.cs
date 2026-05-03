#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1777735779, "DroppedNotNullFromPinCode")]
public class DroppedNotNullFromPinCode : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ALTER COLUMN account_pincode DROP NOT NULL;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ALTER COLUMN account_pincode SET NOT NULL;
        """;
    }
}