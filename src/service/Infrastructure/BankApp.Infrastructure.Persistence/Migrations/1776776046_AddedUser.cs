#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1776776046, "Added user table")]
public class AddedUser : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return
        """
        CREATE TABLE users
        (
            user_id BIGSERIAL PRIMARY KEY, 
            external_id UUID UNIQUE
        );
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return
        """
        DROP TABLE users;
        """;
    }
}