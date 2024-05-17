using Xunit;

namespace MongoDB.Driver;

[CollectionDefinition(nameof(SamplesCollection))]
public class SamplesCollection : ICollectionFixture<SamplesFixture>
{
}
