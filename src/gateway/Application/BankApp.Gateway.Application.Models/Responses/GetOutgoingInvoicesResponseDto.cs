namespace BankApp.Gateway.Application.Models.Responses;

public record GetOutgoingInvoicesResponseDto(IEnumerable<InvoiceDto> Invoices, string? PageToken);