#pragma warning disable SA1649

using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1774272519, "OperationsAdded")]
public sealed class OperationsAdded : IMigration
{
    public void GetUpExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            // language=sql
            SqlStatement = """
            CREATE TYPE OPERATION_TYPE AS ENUM ('deposit_money', 'withdraw_money');           

            CREATE TABLE operations
            (
                operation_id BIGSERIAL NOT NULL PRIMARY KEY,
                operation_type OPERATION_TYPE NOT NULL,
                operation_time TIMESTAMP with time zone NOT NULL,
                account_id BIGSERIAL NOT NULL,
                session_guid UUID NOT NULL
            );
            """,
        });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = """
            DROP TABLE IF EXISTS operations;

            DROP TYPE IF EXISTS OP_TYPE;      
            """,
        });
    }

    public string ConnectionString => throw new NotSupportedException();
}