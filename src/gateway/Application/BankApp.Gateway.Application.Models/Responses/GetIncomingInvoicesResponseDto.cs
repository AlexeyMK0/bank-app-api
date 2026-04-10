namespace BankApp.Gateway.Application.Models.Responses;

public record GetIncomingInvoicesResponseDto(IEnumerable<InvoiceDto> Invoices, string? PageToken);