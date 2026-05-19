namespace IntegrationalTests.Fixtures;

[CollectionDefinition(nameof(WebApplicationCollectionFixture))]
public sealed class WebApplicationCollectionFixture
    : ICollectionFixture<WebApplicationFixture>;