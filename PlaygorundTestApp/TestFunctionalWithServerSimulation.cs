////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using PlaygorundTestApp.FunctionalTesting;
using PlaygorundTestApp.FunctionalTesting.Clients;
using PlaygorundTestApp.FunctionalTesting.Models;
using System.Net;

namespace PlaygorundTestApp
{
    /// <summary>
    /// This class is for functional testing and server simulation.
    /// It tests two methods of CatManager - GetCatProfile and CreateCatWithPhoto
    /// Both the methods make call to the backend API and external API (for photo)
    /// The tests verify that the requests are called in certain order based on condition.
    /// </summary>
    internal class TestFunctionalWithServerSimulation
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

            // test various permutations
            bool result = false;
            result = await Test_Get_Cat_And_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Get_Cat_And_Photo {result}");
            Console.WriteLine("------------------------------------");

            result = await Test_Get_Cat_Invalid_Id().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Get_Cat_Invalid_Id {result}");
            Console.WriteLine("------------------------------------");

            result = await Test_Get_Cat_And_No_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Get_Cat_And_No_Photo {result}");
            Console.WriteLine("------------------------------------");

            result = await Test_Post_Cat_With_Invalid_Id().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_With_Invalid_Id {result}");
            Console.WriteLine("------------------------------------");

            result = await Test_Post_Cat_Valid_Id_No_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_No_Photo {result}");
            Console.WriteLine("------------------------------------");

            result = await Test_Post_Cat_Valid_Id_And_Photo().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_And_Photo {result}");
            Console.WriteLine("------------------------------------");

            // cancel the server listening operation
            cts.Cancel();

            // Cleanup
            _playgroundServer.StopServer();
        }


        // This tests Get Functionality with invalid id
        // it ensures that only one API is called and that is the correct API
        private static async Task<bool> Test_Get_Cat_Invalid_Id()
        {
            // --- Arrange ---
            // Clear all responses
            _playgroundServer.ClearRequestResponses();

            // set the storage folder
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            ServerConfig.StorageFolder = storageFolder;

            // Set Third party API to the playground server base
            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            // --- Act ---
            CatManager catCaller = new();
            CatProfileModel? catProfile = await catCaller.GetCatProfile(-1).ConfigureAwait(false);

            // --- Assert ---
            // functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 1 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/-1"));
        }

        // This tests API sequence when cat is present; but photo is not present in Get method
        private static async Task<bool> Test_Get_Cat_And_No_Photo()
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
            CatProfileModel? catProfile = await catCaller.GetCatProfile(1).ConfigureAwait(false);

            // --- Assert ---
            if (string.IsNullOrEmpty(catProfile?.Name))
            {
                return false;
            }

            if(!string.IsNullOrEmpty(catProfile.PhotoUrl))
            {
                return false;
            }

            // functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/1") &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/1"));
        }

        // This tests scenario when both Cat and Phote exist in testing GetCatProfile
        private static async Task<bool> Test_Get_Cat_And_Photo()
        {
            // --- Arrange ---
            // Clear all responses
            _playgroundServer.ClearRequestResponses();

            // set the storage folder
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            ServerConfig.StorageFolder = storageFolder;

            // Set Third party API to the playground server base
            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            // --- Act ---
            CatManager catCaller = new();
            CatProfileModel? catProfile = await catCaller.GetCatProfile(3).ConfigureAwait(false);

            // --- Assert ---
            if (string.IsNullOrEmpty(catProfile?.Name) || string.IsNullOrEmpty(catProfile.PhotoUrl))
            {
                return false;
            }

            // functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/3") &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/3"));
        }

        // This tests scanario when there is attempt to create cat with invalid id
        private static async Task<bool> Test_Post_Cat_With_Invalid_Id()
        {
            // --- Arrange ---
            // Clear all responses
            _playgroundServer.ClearRequestResponses();

            // set the storage folder
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            ServerConfig.StorageFolder = storageFolder;

            // Set Third party API to the playground server base
            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            // --- Act ---
            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(-1, "Invalid").ConfigureAwait(false);

            if (result != HttpStatusCode.BadRequest)
            {
                return false;
            }

            // --- Assert ---
            //  functional testing to see if there was correct call sequence
            return (_playgroundServer.RequestResponses.Count == 1 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath));
        }

        // This tests scanario when there is attempt to create cat with valid id and no photo
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
