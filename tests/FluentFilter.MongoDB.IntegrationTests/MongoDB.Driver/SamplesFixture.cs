using System;
using System.Threading;
using MongoDB.Driver.Models;

namespace MongoDB.Driver;

public class SamplesFixture : MongoRunnerFixture
{
    public SamplesFixture()
    {
        Database = Client.GetDatabase("tests");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        Database.CreateCollection("Test", cancellationToken: cts.Token);
        Collection = Database.GetCollection<TestModel>("Test");
        Collection.InsertMany(Samples.Instance.Data, cancellationToken: cts.Token);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<TestModel> Collection { get; }
}
