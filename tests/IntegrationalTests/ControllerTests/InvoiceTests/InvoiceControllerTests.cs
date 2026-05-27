using BankApp.Grpc;
using Bogus;
using IntegrationalTests.Fixtures;

namespace IntegrationalTests.ControllerTests.InvoiceTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed partial class InvoiceControllerTests
{
    private const int LocalSeed = 29;

    private readonly WebApplicationFixture _fixture;
    private readonly InvoiceService.InvoiceServiceClient _client;

    private readonly Faker _faker = new Faker()
    {
        Random = new Randomizer(LocalSeed),
    };

    public InvoiceControllerTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = new InvoiceService.InvoiceServiceClient(_fixture.CreateChannel());
    }
}