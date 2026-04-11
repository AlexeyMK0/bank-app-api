namespace BankApp.Gateway.Application.Models;

public sealed record InvoiceDto(long Id, decimal Amount, InvoiceStatusDto State, long RecipientId, long PayerId);