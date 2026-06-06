#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1780518395, "FilledAccountTypeWithPersonal")]
public sealed class FilledAccountTypeWithPersonal : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        UPDATE accounts
        SET account_type = 'personal'
        WHERE account_type IS NULL
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return string.Empty;
    }
}