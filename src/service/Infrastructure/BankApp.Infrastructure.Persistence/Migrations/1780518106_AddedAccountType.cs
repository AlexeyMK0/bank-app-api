#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1780518106, "AddedAccountType")]
public class AddedAccountType : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return
        """
        CREATE TYPE ACCOUNT_TYPE AS ENUM ('corporate', 'personal');

        ALTER TABLE accounts ADD COLUMN account_type ACCOUNT_TYPE; 
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return
        """
        ALTER TABLE accounts DROP COLUMN account_type;

        DROP TYPE IF EXISTS ACCOUNT_TYPE;
        """;
    }
}