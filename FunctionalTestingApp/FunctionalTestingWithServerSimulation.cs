using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using PlaygorundTestApp;
using PlaygorundTestApp.FunctionalTesting;
using PlaygorundTestApp.FunctionalTesting.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FunctionalTestingApp
{
    /// <summary>
    /// This class is for "mini" functional testing and server simulation.
    /// It tests two methods of CatManager - CreateCatWithPhoto
    /// The method makes a call to the backend API and external API (for photo)
    /// The tests verify that the requests are called in certain order based on condition.
    /// </summary>
    internal class FunctionalTestingWithServerSimulation
    {
        private static HttpPlaygoundServer _playgroundServer = new(new MultiServerSimulator());

        internal static async Task Run()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // Server will trigger this after starting
            ManualResetEventSlim serverStarted = new();
            // disable request logging
            _playgroundServer.IsRequestLoggingEnabled = false;

            // run it in a different task/thread
            _ = Task.Run(async () => { await _playgroundServer.StartServer(serverStarted, cts.Token).ConfigureAwait(false); });

            // wait for the server to start
            serverStarted.Wait();

            // Workflow testing when photo is not available
            bool result = await Test_Post_Cat_Valid_Id_No_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_No_Photo {result}");
            Console.WriteLine("------------------------------------");

            // Workflow testing when photo is available
            result = await Test_Post_Cat_Valid_Id_And_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_And_Photo {result}");
            Console.WriteLine("------------------------------------");

            // for more examples check GitHub

            // cancel the server listening operation
            cts.Cancel();

            // Cleanup
            _playgroundServer.StopServer();
        }

        private static async Task<bool> Test_Post_Cat_Valid_Id_No_Photo()
        {
            // --- Arrange ---
            // Clear all responses
            _playgroundServer.ClearRequestResponses();

            // set the storage folder
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            ServerConfig.StorageFolder = storageFolder;

            // Set Third party API to the mock server base
            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            // --- Act ---
            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(1, "No Photo").ConfigureAwait(false);

            // --- Assert ---
            if (result != HttpStatusCode.Created)
            {
                return false;
            }

            // functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/1"));
        }

        // This tests scanario when there is attempt to create cat with photo
        private static async Task<bool> Test_Post_Cat_Valid_Id_And_Photo()
        {
            // --- Arrange ---
            // Clear all responses
            _playgroundServer.ClearRequestResponses();

            // set the storage folder
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            ServerConfig.StorageFolder = storageFolder;

            // Set Third party API to the mock server base
            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            // --- Act ---
            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(3, "With Photo").ConfigureAwait(false);

            if (result != HttpStatusCode.OK)
            {
                return false;
            }

            // --- Assert ---
            // functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 3 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/3") &&
                ValidateRequest(_playgroundServer.RequestResponses[2].Item1, HttpMethod.Patch, RequestSender.RestPath));

        }

        // validates verb and URL of the request
        private static bool ValidateRequest(RequestModel req, HttpMethod verb, string urlEnd = RequestSender.RestPath)
        {
            return req.Verb == verb.Method &&
                   req.URL.EndsWith(urlEnd);
        }

    }
}
