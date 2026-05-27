using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Invoices;
using Moq;

namespace UnitTests.Specifications;

public static class InvoiceRepositoryMockSpecification
{
    public static Mock<IInvoiceRepository> SetupQueryByInvoiceId(
        this Mock<IInvoiceRepository> mock,
        InvoiceId invoiceId,
        IEnumerable<Invoice> invoices)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<InvoiceQuery>(query => Enumerable.Contains(query.InvoiceIds, invoiceId)),
                    It.IsAny<CancellationToken>()))
            .Returns(invoices.ToAsyncEnumerable());

        return mock;
    }

    public static void SetupUpdateWithChangedState(
        this Mock<IInvoiceRepository> mock,
        Invoice invoiceToUpdate,
        InvoiceStatus updatedStatus)
    {
        mock.Setup(repo => repo
                .UpdateAsync(
                    It.Is<Invoice>(inv => inv.Amount == invoiceToUpdate.Amount
                                          && inv.RecipientId == invoiceToUpdate.RecipientId
                                          && inv.PayerId == invoiceToUpdate.PayerId
                                          && inv.State.Status == updatedStatus),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice newInvoice, CancellationToken token) => newInvoice);
    }

    public static void SetupQueryByQuery(
        this Mock<IInvoiceRepository> mock,
        InvoiceQuery query,
        IEnumerable<Invoice> invoices)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<InvoiceQuery>(q =>
                        q.KeyCursor == query.KeyCursor
                         && q.InvoiceIds.ToHashSet().SetEquals(query.InvoiceIds)
                         && q.PageSize == query.PageSize
                         && q.Payers.ToHashSet().SetEquals(query.Payers)
                         && q.Recipients.ToHashSet().SetEquals(query.Recipients)
                         && q.Statuses.ToHashSet().SetEquals(query.Statuses)),
                    It.IsAny<CancellationToken>()))
            .Returns(invoices.ToAsyncEnumerable());
    }
}