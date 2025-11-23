////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;

namespace PlaygorundTestApp;

/// <summary>
/// This app is for testing the server. It tests various permutation, combinations.
/// It integrates directly with code.
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        // Loads values from appsettings.json
        LoadConfig();

        // start the server
        CancellationTokenSource cts = new CancellationTokenSource();
        ManualResetEventSlim serverStarted = new();

        HttpPlaygoundServer httpTestListener = new();
        Task httpListener = Task.Run(() => httpTestListener.StartHttpListner(serverStarted, cts.Token));

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

        result = await TestDataWithQueryParam().ConfigureAwait(false);
        Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with Query param returned {result}");
        Console.WriteLine("------------------------------------");

        result = await TestDataWithInvalidFiles().ConfigureAwait(false);
        Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with Invalid Files returned {result}");
        Console.WriteLine("------------------------------------");

        result = await TestDataWithNonJsonFiles().ConfigureAwait(false);
        Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Testing Data with non Json files  returned {result}");
        Console.WriteLine("------------------------------------");

        cts.Cancel();

        await httpListener;
    }

    private static async Task<bool> TestWithEmptyFolder()
    {
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDataEmpty");
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
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDataNoParam");
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

    private static async Task<bool> TestDataWithQueryParam()
    {
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDataWithQueryParam");
        // set the storage folder
        ServerConfig.StorageFolder = storageFolder;

        // send all requests
        RequestSender requestSender = new();

        (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet(respFile: "Foo.json").ConfigureAwait(false);
        if (!CheckCustomResponse(statusCode, body))
        {
            return false;
        }

        (statusCode, body) = await requestSender.SendDelete(respFile: "Foo.json").ConfigureAwait(false);
        if (!CheckCustomResponse(statusCode, body))
        {
            return false;
        }

        (statusCode, body) = await requestSender.SendPost(respFile: "Foo.json").ConfigureAwait(false);
        if (!CheckCustomResponse(statusCode, body))
        {
            return false;
        }

        (statusCode, body) = await requestSender.SendPut(respFile: "Foo.json").ConfigureAwait(false);
        if (!CheckCustomResponse(statusCode, body))
        {
            return false;
        }

        (statusCode, body) = await requestSender.SendPatch(respFile: "Foo.json").ConfigureAwait(false);
        if (!CheckCustomResponse(statusCode, body))
        {
            return false;
        }

        (statusCode, _, _) = await requestSender.SendGet(respFile: "Bar.json").ConfigureAwait(false);
        if (statusCode != HttpStatusCode.NotFound)
        {
            return false;
        }

        return true;

        bool CheckCustomResponse(HttpStatusCode statusCode, string body)
        {
            if (statusCode != HttpStatusCode.OK)
            {
                return false;
            }

            CatModel cat = JsonSerializer.Deserialize<CatModel>(body);
            if (cat == null || cat.Id != 48 || cat.Name != "Foo")
            {
                return false;
            }

            return true;
        }
    }

    private static async Task<bool> TestDataWithInvalidFiles()
    {
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDataInvalidFiles");
        // set the storage folder
        ServerConfig.StorageFolder = storageFolder;

        // send all requests
        RequestSender requestSender = new();

        (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet(respFile: "Invalid.json").ConfigureAwait(false);
        if (statusCode != HttpStatusCode.InternalServerError)
        {
            return false;
        }
        Console.WriteLine($"Error Message {body}");
        Console.WriteLine();

        (statusCode, _, body) = await requestSender.SendGet(respFile: "InvalidStatusCode.json").ConfigureAwait(false);
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
        string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDataNonJson");
        // set the storage folder
        ServerConfig.StorageFolder = storageFolder;

        // send all requests
        RequestSender requestSender = new();

        (HttpStatusCode statusCode, Dictionary<string, string> headers, string body) = await requestSender.SendGet(respFile: "html.json").ConfigureAwait(false);
        if (statusCode != HttpStatusCode.OK)
        {
            return false;
        }
        Console.WriteLine($"HTML is {body}");
        Console.WriteLine();

        (statusCode, _, body) = await requestSender.SendGet(respFile: "Plain.json").ConfigureAwait(false);
        if (statusCode != HttpStatusCode.OK)
        {
            return false;
        }
        Console.WriteLine($"Plain text is {body}");
        Console.WriteLine();

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

    public static void LoadConfig(string configFilePath = "appsettings.json")
    {
        if (File.Exists(configFilePath))
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            ServerConfig.HostName = configuration["AppConfig:HostName"] ?? string.Empty;
            ServerConfig.Port = int.TryParse(configuration["AppConfig:Port"], out int portValue) ? portValue : 0;
            ServerConfig.StorageFolder = configuration["AppConfig:StorageFolder"] ?? string.Empty;
        }
    }
}
