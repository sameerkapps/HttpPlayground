////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using System.Net;
using System.Text.Json;

namespace PlaygorundTestApp
{
    /// <summary>
    /// This is for testing with server logic simulator. 
    /// The logic simulates condition that if cat's name is offensive, it will return bad request.
    /// </summary>
    internal class TestWithServerSimulation
    {
        internal static async Task Run()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // Server will trigger this after starting
            ManualResetEventSlim serverStarted = new();

            // create playground server passing Server Logic simulator as parameter
            HttpPlaygoundServer playgroundServer = new(new ServerLogicSimulator());

            // run it in a different task/thread
            _ = Task.Run(async () => { await playgroundServer.StartServer(serverStarted, cts.Token).ConfigureAwait(false); });

            // wait for the server to start
            serverStarted.Wait();

            // test
            bool result = false;
            result = await TestWithServerSimulationFolder().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing with TestWithServerSimulationFolder returned {result}");
            Console.WriteLine("------------------------------------");

            // cancel the server listening operation
            cts.Cancel();

            // clean up
            playgroundServer.StopServer();
        }

        private static async Task<bool> TestWithServerSimulationFolder()
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/ServerSimulation");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // empty Requests subfolder
            string requestsFolder = Path.Combine(storageFolder, RequestSender.RestPath, "Requests");
            if (Directory.Exists(requestsFolder))
            {
                Directory.Delete(requestsFolder, true);
            }

            // Create helper class
            RequestSender requestSender = new();

            // Send create Request with non offensive name
            (HttpStatusCode statusCode, _) = await requestSender.SendPost().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.Created)
            {
                return false;
            }

            // create cat model with offensive name
            CatModel cat = new CatModel()
            {
                Id = 420,
                Name = "Offensive"
            };

            // send create request with offensive name
            (statusCode, _) = await requestSender.SendPost(cat).ConfigureAwait(false);

            // verify that return code is bad request
            if (statusCode != HttpStatusCode.BadRequest)
            {
                return false;
            }

            // ensure that correct files are created
            return (GetRequestFilesCount(requestsFolder) == 2);
        }

        private static int GetRequestFilesCount(string requestsFolder)
        {
            string fileSuffix = $"-{DateTime.Now.ToString("yyyyMMdd-")}*.json";

            string postFile = $"POST{fileSuffix}";

            return Directory.GetFiles(requestsFolder, postFile).Count();
        }

        /// <summary>
        /// This class will simulate logic to prevent creation of cat with offensive name
        /// </summary>
        class ServerLogicSimulator : HttpRequestProcessor
        {
            /// <summary>
            /// This wiill simulate server logic. In this case, if Cat's name is offensive, it will return 400
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            protected override Task<ResponseModel> SimulateServerHandling(RequestModel rs)
            {
                CatModel? cat = JsonSerializer.Deserialize<CatModel>(rs.Body);

                if (string.Equals(cat?.Name, "Offensive"))
                {
                    return base.RetrieveByFile("400.json");
                }
                else
                {
                    return base.SimulateServerHandling(rs);
                }

            }
        }
    }
}
