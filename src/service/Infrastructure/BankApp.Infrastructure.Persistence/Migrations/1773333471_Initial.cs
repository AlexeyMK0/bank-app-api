#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace BankApp.Infrastructure.Persistence.Migrations;

[Migration(1773333471, "Initial")]
public class Initial : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return """
        CREATE TABLE accounts
        (
            account_id BIGSERIAL PRIMARY KEY,
            account_pincode TEXT NOT NULL,
            account_balance decimal NOT NULL
        );

        CREATE TABLE admin_sessions
        (
            session_id UUID PRIMARY KEY
        );

        CREATE TABLE user_sessions
        (
            session_id UUID PRIMARY KEY,
            account_id BIGINT NOT NULL
        );
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """
        DROP TABLE user_sessions;
        DROP TABLE admin_sessions;
        DROP TABLE operations;
        DROP TABLE accounts;
        """;
    }
}