#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1775124185, "DropAmountAndTypeFromOperation")]
public class DropAmountAndTypeFromOperation : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        // language=sql
        return """
        ALTER TABLE operations DROP COLUMN amount;

        ALTER TABLE operations DROP COLUMN operation_type;

        DROP TYPE OPERATION_TYPE;
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        // TODO: somehow save invoice operations
        // language=sql
        return """
        DELETE FROM operations WHERE jsonb->>'$type' NOT IN ('DepositMoney', 'WithdrawMoney');

        CREATE TYPE OPERATION_TYPE AS ENUM ('deposit_money', 'withdraw_money');

        UPDATE operations
        SET
            operation_type = CASE jsonb->>'$type'
                                 WHEN 'DepositMoney' THEN 'deposit_money'::OPERATION_TYPE
                                 WHEN 'WithdrawMoney' THEN 'withdraw_money'::OPERATION_TYPE
                             END,
            amount = (jsonb->>'amount')::decimal;
        """;
    }
}