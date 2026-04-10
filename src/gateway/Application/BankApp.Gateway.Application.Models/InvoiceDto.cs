namespace BankApp.Gateway.Application.Models;

public record InvoiceDto(long Id, decimal Amount, InvoiceStatusDto State, long RecipientId, long PayerId);