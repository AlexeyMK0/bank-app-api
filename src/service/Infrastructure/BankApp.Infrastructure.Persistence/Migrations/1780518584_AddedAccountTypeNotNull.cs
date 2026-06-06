#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1780518584, "AddedAccountTypeNotNull")]
public class AddedAccountTypeNotNull : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return """ALTER TABLE accounts ALTER COLUMN account_type SET NOT NULL;""";
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """ALTER TABLE accounts ALTER COLUMN account_type DROP NOT NULL;""";
    }
}