using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using PlaygorundTestApp.FunctionalTesting;
using PlaygorundTestApp.FunctionalTesting.Clients;
using PlaygorundTestApp.FunctionalTesting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlaygorundTestApp
{
    internal class TestFunctionalWithServerSimulation
    {
        private static HttpPlaygoundServer _playgroundServer;
        internal static async Task Run()
        {
            // start the server
            CancellationTokenSource cts = new CancellationTokenSource();
            ManualResetEventSlim serverStarted = new();

            _playgroundServer = new(new MultiServerSimulator());
            _playgroundServer.IsRequestLoggingEnabled = false;

            Task<HttpListener> httpListener = Task.Run<HttpListener>(async () => { return await _playgroundServer.StartHttpListner(serverStarted, cts.Token).ConfigureAwait(false); });

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

            cts.Cancel();

            await httpListener;

            httpListener.Result.Stop();
            httpListener.Result.Close();
        }

        private static async Task<bool> Test_Get_Cat_Invalid_Id()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            CatProfieModel? catProfile = await catCaller.GetCatProfile(-1).ConfigureAwait(false);

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 1 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/-1"));
        }

        private static async Task<bool> Test_Get_Cat_And_No_Photo()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            CatProfieModel? catProfile = await catCaller.GetCatProfile(1).ConfigureAwait(false);

            if (string.IsNullOrEmpty(catProfile.Name))
            {
                return false;
            }

            if(!string.IsNullOrEmpty(catProfile.PhotoUrl))
            {
                return false;
            }

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/1") &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/1"));
        }

        private static async Task<bool> Test_Get_Cat_And_Photo()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            CatProfieModel? catProfile = await catCaller.GetCatProfile(3).ConfigureAwait(false);

            if(string.IsNullOrEmpty(catProfile.Name) || string.IsNullOrEmpty(catProfile.PhotoUrl))
            {
                return false;
            }

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Get, RequestSender.RestPath + "/3") &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/3"));
        }

        private static async Task<bool> Test_Post_Cat_With_Invalid_Id()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(-1, "Invalid").ConfigureAwait(false);

            if (result != HttpStatusCode.BadRequest)
            {
                return false;
            }

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 1 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath));
        }

        private static async Task<bool> Test_Post_Cat_Valid_Id_No_Photo()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(1, "No Photo").ConfigureAwait(false);

            if (result != HttpStatusCode.Created)
            {
                return false;
            }

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 2 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/1"));
        }

        private static async Task<bool> Test_Post_Cat_Valid_Id_And_Photo()
        {
            _playgroundServer.ClearRequestResponses();

            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
            PhotoClient.Instance.Port = ServerConfig.Port;

            CatManager catCaller = new();
            HttpStatusCode result = await catCaller.CreateCatWithPhoto(3, "With Photo").ConfigureAwait(false);

            if (result != HttpStatusCode.OK)
            {
                return false;
            }

            // now functional testing to see if there was correct call sequence
            Console.WriteLine($"Request Sequence...");
            foreach (var pair in _playgroundServer.RequestResponses)
            {
                RequestModel request = pair.Item1;
                ResponseModel response = pair.Item2;

                Console.WriteLine($"{request.URL}");
            }

            return (_playgroundServer.RequestResponses.Count == 3 &&
                ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
                ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/3") &&
                ValidateRequest(_playgroundServer.RequestResponses[2].Item1, HttpMethod.Patch, RequestSender.RestPath));
        }

        private static bool ValidateRequest(RequestModel req, HttpMethod verb, string urlEnd = RequestSender.RestPath)
        {
            return req.Verb == verb.Method &&
                   req.URL.EndsWith(urlEnd);
        }
    }
}
