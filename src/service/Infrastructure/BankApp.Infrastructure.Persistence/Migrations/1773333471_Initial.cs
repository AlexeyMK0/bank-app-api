#pragma warning disable SA1649

using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1773333471, "Initial")]
public class Initial1773333471 : IMigration
{
    public void GetUpExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = """
            CREATE TABLE accounts
            (
                account_id BIGSERIAL NOT NULL PRIMARY KEY,
                account_pincode TEXT NOT NULL,
                account_balance decimal NOT NULL
            );

            CREATE TABLE admin_sessions
            (
                session_guid UUID PRIMARY KEY
            );

            CREATE TABLE user_sessions
            (
                session_guid UUID PRIMARY KEY,
                account_id BIGSERIAL NOT NULL
            );
""",
        });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = """
            DROP TABLE user_sessions;
            DROP TABLE admin_sessions;
            DROP TABLE operations;
            DROP TABLE accounts;
            """,
        });
    }

    public string ConnectionString => throw new NotSupportedException();
}