using System;
using System.Diagnostics;
using EphemeralMongo;

namespace MongoDB.Driver;

public class MongoRunnerFixture : IDisposable
{
    public IMongoRunner Runner { get; }

    public MongoClient Client { get; }

    public MongoRunnerFixture()
    {
        // All properties below are optional. The whole "options" instance is optional too!
        var options = new MongoRunnerOptions {
            UseSingleNodeReplicaSet = true, // Default: false
            StandardOuputLogger = line => Debug.WriteLine(line), // Default: null
            StandardErrorLogger = line => Debug.WriteLine(line), // Default: null
            // DataDirectory = "/path/to/data/", // Default: null
            // BinaryDirectory = "/path/to/mongo/bin/", // Default: null
            ConnectionTimeout = TimeSpan.FromSeconds(10), // Default: 30 seconds
            ReplicaSetSetupTimeout = TimeSpan.FromSeconds(5), // Default: 10 seconds
            AdditionalArguments = "--quiet", // Default: null
            MongoPort = null, // Default: random available port

            // EXPERIMENTAL - Only works on Windows and modern .NET (netcoreapp3.1, net5.0, net6.0, net7.0 and so on):
            // Ensures that all MongoDB child processes are killed when the current process is prematurely killed,
            // for instance when killed from the task manager or the IDE unit tests window. Processes are managed as a unit using
            // job objects: https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
            KillMongoProcessesWhenCurrentProcessExits = true // Default: false
        };

        Runner = MongoRunner.Run(options);
        Client = new MongoClient(Runner.ConnectionString);
    }

    public void Dispose()
    {
        Runner.Dispose();
    }
}
