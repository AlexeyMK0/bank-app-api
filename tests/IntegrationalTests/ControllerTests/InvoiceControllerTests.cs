#pragma warning disable IDE0052

using BankApp.Grpc;
using IntegrationalTests.Fixtures;

namespace IntegrationalTests.ControllerTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class InvoiceControllerTests
{
    private readonly WebApplicationFixture _fixture;
    private readonly AccountService.AccountServiceClient _client;

    public InvoiceControllerTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = new AccountService.AccountServiceClient(_fixture.CreateChannel());
    }
}