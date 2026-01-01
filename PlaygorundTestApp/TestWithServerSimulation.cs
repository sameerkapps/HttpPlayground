using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaygorundTestApp
{
    internal class TestWithServerSimulation
    {
        internal static async Task Run()
        {
            // start the server
            CancellationTokenSource cts = new CancellationTokenSource();
            ManualResetEventSlim serverStarted = new();

            HttpPlaygoundServer httpTestListener = new(new ServerLogicSimulator());
            Task<HttpListener> httpListener = Task.Run<HttpListener>(async () => { return await httpTestListener.StartHttpListner(serverStarted, cts.Token).ConfigureAwait(false); });

            // wait for the server to start
            serverStarted.Wait();

            // test various permutations
            bool result = false;
            result = await TestWithServerSimulationFolder().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing with empty folder returned {result}");
            Console.WriteLine("------------------------------------");

            cts.Cancel();

            await httpListener;

            httpListener.Result.Stop();
            httpListener.Result.Close();
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

            // send all requests
            RequestSender requestSender = new();

            // Send Request with non offensive name
            (HttpStatusCode statusCode, _) = await requestSender.SendPost().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.Created)
            {
                return false;
            }

            CatModel cat = new CatModel()
            {
                Id = 420,
                Name = "Offensive"
            };

            // send post with offensive name
            (statusCode, _) = await requestSender.SendPost(cat).ConfigureAwait(false);
            if (statusCode != HttpStatusCode.BadRequest)
            {
                return false;
            }

            // ensure that files are created

            return (GetRequestFilesCount(requestsFolder) == 2);
        }

        private static int GetRequestFilesCount(string requestsFolder)
        {
            string fileSuffix = $"-{DateTime.Now.ToString("yyyyMMdd-")}*.json";

            string postFile = $"POST{fileSuffix}";

            return Directory.GetFiles(requestsFolder, postFile).Count();
        }

        class ServerLogicSimulator : HttpRequestProcessor
        {
            /// <summary>
            /// This wiill simulate server logic. In this case, if Cat's name is offensive, it will return 400
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            protected override Task<ResponseModel> SimulateServerHandling(RequestModel rs)
            {
                CatModel cat = JsonSerializer.Deserialize<CatModel>(rs.Body);

                if (string.Equals(cat.Name, "Offensive"))
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
