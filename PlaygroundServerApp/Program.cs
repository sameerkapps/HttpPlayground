////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;

namespace PlaygroundServerApp;

/// <summary>
/// This app starts an HTTP Playground Server for handling requests.
/// </summary>
/// <remarks>This method configures the server's storage folder, starts the HTTP server, and waits for it
/// to begin listening for requests. The server runs until the user presses Enter, at which point the server is
/// stopped gracefully.</remarks>
internal class Program
{
    static async Task Main(string[] args)
    {
        // cancellation token for the server loop
        CancellationTokenSource cts = new CancellationTokenSource();

        // this is signaled when the server starts
        ManualResetEventSlim serverStarted = new();

        // Update server storage folder
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        // set the storage folder
        ServerConfig.StorageFolder = storageFolder;

        Console.WriteLine($"Storage Folder is {ServerConfig.StorageFolder}");

        // instantiate and run the server
        HttpPlaygoundServer httpTestListener = new();
        Task httpListener = Task.Run(() => httpTestListener.StartServer(serverStarted, cts.Token));

        // wait for server to start
        serverStarted.Wait();

        // When user presses enter stop the server
        Console.WriteLine("Server has started listening. You may begin requests...");
        Console.WriteLine("Press Enter to stop the server....");
        Console.ReadLine();

        cts.Cancel();

        await httpListener;
    }
}
