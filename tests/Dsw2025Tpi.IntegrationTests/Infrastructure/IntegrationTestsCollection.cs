namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Todas las clases marcadas con [Collection(IntegrationTestsCollection.Name)] comparten
/// una única instancia de <see cref="CustomWebApplicationFactory"/> (y por ende la misma base).
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}
