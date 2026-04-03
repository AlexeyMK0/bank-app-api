#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1775114439, "AddedAmountToOperations")]
public class AddedAmountToOperations1775114439 : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return """
        ALTER TABLE operations
        ADD COLUMN amount decimal NOT NULL DEFAULT 0.00;
        
        ALTER TABLE operations
        ALTER COLUMN amount DROP DEFAULT;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """
        ALTER TABLE operations
           DROP COLUMN amount
        """;
    }
}