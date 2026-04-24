#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

// applying this migration requires data-fix
[Migration(1777021141, "AccountUserIdNotNullConstraint")]
public class AccountUserIdNotNullConstraint : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ALTER COLUMN user_id SET NOT NULL;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ALTER COLUMN user_id DROP NOT NULL;
        """;
    }
}