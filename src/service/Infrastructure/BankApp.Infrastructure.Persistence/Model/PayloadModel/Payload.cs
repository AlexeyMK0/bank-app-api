using System.Text.Json.Serialization;

namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

[JsonDerivedType(typeof(WithdrawPayload), typeDiscriminator: "Withdraw")]
[JsonDerivedType(typeof(DepositPayload), typeDiscriminator: "Deposit")]
[JsonDerivedType(typeof(PayInvoicePayload), typeDiscriminator: "PayInvoice")]
[JsonDerivedType(typeof(PaymentReceivedPayload), typeDiscriminator: "PaymentReceived")]
public record Payload;