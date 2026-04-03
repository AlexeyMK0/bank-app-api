#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1774272519, "OperationsAdded")]
public sealed class OperationsAdded : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return """
        CREATE TYPE OPERATION_TYPE AS ENUM ('deposit_money', 'withdraw_money');           

        CREATE TABLE operations
        (
           operation_id BIGSERIAL NOT NULL PRIMARY KEY,
           operation_type OPERATION_TYPE NOT NULL,
           operation_time TIMESTAMP with time zone NOT NULL,
           account_id BIGSERIAL NOT NULL,
           session_guid UUID NOT NULL
        );
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """
        DROP TABLE IF EXISTS operations;

        DROP TYPE IF EXISTS OPERATION_TYPE;      
        """;
    }
}