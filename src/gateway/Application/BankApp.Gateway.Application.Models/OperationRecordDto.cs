using BankApp.Gateway.Application.Models.Operations;
using System.Text.Json.Serialization;

namespace BankApp.Gateway.Application.Models;

[JsonDerivedType(typeof(DepositOperationDto), nameof(DepositOperationDto))]
[JsonDerivedType(typeof(WithdrawOperationDto), nameof(WithdrawOperationDto))]
[JsonDerivedType(typeof(PayInvoiceOperationDto), nameof(PayInvoiceOperationDto))]
[JsonDerivedType(typeof(PaymentReceivedOperationDto), nameof(PaymentReceivedOperationDto))]
public abstract record OperationRecordDto(
    long Id,
    DateTimeOffset Time,
    long AccountId);