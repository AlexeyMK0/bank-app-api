#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1776776785, "AddedNullableUserIdToAccount")]
public class AddedNullableUserIdToAccount : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts ADD COLUMN user_id BIGINT;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        ALTER TABLE accounts DROP COLUMN user_id;
        """;
    }
}