using HttpPlaygroundServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaygorundTestApp
{
    internal class TestWithNoServerSimulation
    {
        internal static async Task Run()
        {
            // start the server
            CancellationTokenSource cts = new CancellationTokenSource();
            ManualResetEventSlim serverStarted = new();

            HttpPlaygoundServer playgroundServer = new();
            Task<HttpListener> httpListener = Task.Run<HttpListener>(async () => { return await playgroundServer.StartHttpListner(serverStarted, cts.Token).ConfigureAwait(false); });

            // wait for the server to start
            serverStarted.Wait();

            // test various permutations
            bool result = false;
            result = await TestWithEmptyFolder().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing with empty folder returned {result}");
            Console.WriteLine("------------------------------------");

            result = await TestDataWithNoParam().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with no param returned {result}");
            Console.WriteLine("------------------------------------");

            result = await TestDataWithInvalidFiles().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with Invalid Files returned {result}");
            Console.WriteLine("------------------------------------");

            result = await TestDataWithNonJsonFiles().ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with non Json files  returned {result}");
            Console.WriteLine("------------------------------------");

            result = await TestEnableLogging(playgroundServer).ConfigureAwait(false);
            Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing EnableLogging returned {result}");
            Console.WriteLine("------------------------------------");

            cts.Cancel();

            await httpListener;

            httpListener.Result.Stop();
            httpListener.Result.Close();
        }

        private static async Task<bool> TestWithEmptyFolder()
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Empty");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // empty Requests subfolder
            string requestsFolder = Path.Combine(storageFolder, RequestSender.RestPath, "Requests");
            if (Directory.Exists(requestsFolder))
            {
                Directory.Delete(requestsFolder, true);
            }

            string requests1Folder = Path.Combine(storageFolder, RequestSender.RestPath, "1", "Requests");
            if (Directory.Exists(requests1Folder))
            {
                Directory.Delete(requests1Folder, true);
            }

            // send all requests
            RequestSender requestSender = new();

            (HttpStatusCode statusCode, Dictionary<string, string> _, string body) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            (statusCode, _, body) = await requestSender.SendGet("/1").ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendDelete().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.NoContent)
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPost().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.Created)
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPut().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPatch().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            // ensure that files are created
            string file1Suffix = $"GET-{DateTime.Now.ToString("yyyyMMdd-")}*.json";

            return AreRequestFilesCreated(requestsFolder) && Directory.GetFiles(requests1Folder, file1Suffix).Any();
        }

        private static async Task<bool> TestDataWithNoParam()
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/NoParam");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // send all requests
            RequestSender requestSender = new();

            (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            // walk through the headers and make sure that there is custom header
            if (!headers.TryGetValue("CustomHdr", out string? hdrValue))
            {
                return false;
            }

            if (hdrValue != "Cats are fun")
            {
                return false;
            }

            List<CatModel> cats = JsonSerializer.Deserialize<List<CatModel>>(body);
            if (cats == null || cats.Count != 2)
            {
                return false;
            }
            if (!(cats[0].Id == 22 && cats[1].Id == 33 &&
                  cats[0].Name == "Blue" && cats[1].Name == "Red"))
            {
                return false;
            }

            (statusCode, _, body) = await requestSender.SendGet("/1").ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            CatModel cat1 = JsonSerializer.Deserialize<CatModel>(body);
            if (cat1 == null || cat1.Id != 1 || cat1.Name != "First")
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendDelete().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            CatModel cat = JsonSerializer.Deserialize<CatModel>(body);
            if (cat == null || cat.Id != 42 || cat.Name != "Never")
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPost().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.Created)
            {
                return false;
            }

            cat = JsonSerializer.Deserialize<CatModel>(body);
            if (cat == null || cat.Id != 99 || cat.Name != "Purple")
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPut().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            cat = JsonSerializer.Deserialize<CatModel>(body);
            if (cat == null || cat.Id != 109 || cat.Name != "Purr")
            {
                return false;
            }

            (statusCode, body) = await requestSender.SendPatch().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            cat = JsonSerializer.Deserialize<CatModel>(body);
            if (cat == null || cat.Id != 99 || cat.Name != "Pat")
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> TestDataWithInvalidFiles()
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/InvalidFiles");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // send all requests
            RequestSender requestSender = new();

            (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.InternalServerError)
            {
                return false;
            }
            Console.WriteLine($"Error Message {body}");
            Console.WriteLine();

            (statusCode, body) = await requestSender.SendDelete().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.InternalServerError)
            {
                return false;
            }
            Console.WriteLine($"Error Message {body}");
            Console.WriteLine();

            return true;
        }

        private static async Task<bool> TestDataWithNonJsonFiles()
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/NonJson");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // send all requests
            RequestSender requestSender = new();

            // Get HTML file
            (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            Console.WriteLine($"HTML is {body}");
            Console.WriteLine();

            // Get Plain text
            (statusCode, _, body) = await requestSender.SendGet("/1").ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }
            Console.WriteLine($"Plain text is {body}");
            Console.WriteLine();

            return true;
        }

        private static async Task<bool> TestEnableLogging(HttpPlaygoundServer playgroundServer)
        {
            string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Empty");
            // set the storage folder
            ServerConfig.StorageFolder = storageFolder;

            // empty Requests subfolder
            string requestsFolder = Path.Combine(storageFolder, RequestSender.RestPath, "Requests");
            if (Directory.Exists(requestsFolder))
            {
                Directory.Delete(requestsFolder, true);
            }

            playgroundServer.IsRequestLoggingEnabled = true;
            // send all requests
            RequestSender requestSender = new();

            (HttpStatusCode statusCode, Dictionary<string, string> _, _) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            if(Directory.GetFiles(requestsFolder).Count() == 0)
            {
                Console.WriteLine("Failed to create Request logs.");

                return false;
            }

            if (Directory.Exists(requestsFolder))
            {
                Directory.Delete(requestsFolder, true);
            }

            playgroundServer.IsRequestLoggingEnabled = false;
            (statusCode,  _, _) = await requestSender.SendGet().ConfigureAwait(false);
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            if (Directory.Exists(requestsFolder))
            {
                Console.WriteLine("Failed to disable logs.");

                return false;
            }

            return true;
        }

        private static bool AreRequestFilesCreated(string requestsFolder)
        {
            string fileSuffix = $"-{DateTime.Now.ToString("yyyyMMdd-")}*.json";

            string getFile = $"GET{fileSuffix}";
            string putFile = $"PUT{fileSuffix}";
            string postFile = $"POST{fileSuffix}";
            string patchFile = $"PATCH{fileSuffix}";
            string deleteFile = $"DELETE{fileSuffix}";

            return FileExists(requestsFolder, getFile) &&
                   FileExists(requestsFolder, putFile) &&
                   FileExists(requestsFolder, postFile) &&
                   FileExists(requestsFolder, patchFile) &&
                   FileExists(requestsFolder, deleteFile);

            bool FileExists(string folder, string pattern)
            {
                return Directory.GetFiles(folder, pattern).Any();
            }
        }
    }
}
