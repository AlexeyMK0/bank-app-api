#pragma warning disable SA1649

using FluentMigrator;
using Itmo.Dev.Platform.Persistence.Postgres.Migrations;

namespace Lab1.Infrastructure.Persistence.Migrations;

[Migration(1775124131, "AddedJsonbToOperations")]
public class AddedJsonbToOperations1775124131 : SqlMigration
{
    protected override string GetUpSql(IServiceProvider serviceProvider)
    {
        return """
        ALTER TABLE operations ADD COLUMN payload jsonb;

        UPDATE operations
        SET payload = jsonb_build_object(
                      'type', CASE operation_type
                        WHEN 'deposit_money' THEN 'DepositMoney'
                        WHEN 'withdraw_money' THEN 'WithdrawMoney'
                      END,
                      'amount', amount
        )
        WHERE payload IS NULL
        """;
    }

    protected override string GetDownSql(IServiceProvider serviceProvider)
    {
        return """
        ALTER TABLE operations DROP COLUMN payload
        """;
    }
}