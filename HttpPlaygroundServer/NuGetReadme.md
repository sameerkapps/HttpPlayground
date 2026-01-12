# HttpPlaygroundServer

Enables **functional testing of HTTP client workflows** using a local server that records request–response pairs and provides the ability to verify call sequences.

---

## Functional testing: the primary use case

HttpPlaygroundServer is designed for **“mini” functional testing** of HTTP client workflows—especially when your code calls **multiple backends** (e.g., your API + a 3rd‑party API) and you want to validate:

- **Call order** (sequence of requests)
- **Branching behavior** (different sequences based on conditions)
- **Real HTTP plumbing** (URLs, headers, serialization) without a live backend
- **Server simulation** (custom logic or file-based responses)

This package is intended for **console apps and custom harnesses** (it does not require or integrate with xUnit/NUnit).

---

## Installation

```bash
dotnet add package sameerk.HttpPlaygroundServer
```

---

## Functional testing with server simulation (sample)

This is a simplified excerpt of the sample you provided. It demonstrates:

- Starting the playground server with a **custom simulator**
- Running a workflow twice (photo missing vs. photo available)
- Verifying the **sequence of recorded request–response pairs** via `RequestResponses`

### Start the server and run functional workflows

```csharp
/// <summary>
/// "Mini" functional testing + server simulation:
/// Tests CatManager.CreateCatWithPhoto which calls a backend API + external photo API.
/// Verifies requests are called in a certain order based on conditions.
/// </summary>
internal class FunctionalTestingWithServerSimulation
{
    private static HttpPlaygoundServer _playgroundServer = new(new MultiServerSimulator());

    internal static async Task Run()
    {
        CancellationTokenSource cts = new();
        ManualResetEventSlim serverStarted = new();

        // optional: disable request logging to disk
        _playgroundServer.IsRequestLoggingEnabled = false;

        _ = Task.Run(async () =>
            await _playgroundServer.StartServer(serverStarted, cts.Token).ConfigureAwait(false)
        );

        serverStarted.Wait();

        // Workflow: photo NOT available
        bool result = await Test_Post_Cat_Valid_Id_No_Photo().ConfigureAwait(false);
        Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_No_Photo {result}");

        // Workflow: photo available
        result = await Test_Post_Cat_Valid_Id_And_Photo().ConfigureAwait(false);
        Console.WriteLine($"{(result ? string.Empty : "--- FAILED- -->")} Test_Post_Cat_Valid_Id_And_Photo {result}");

        cts.Cancel();
        _playgroundServer.StopServer();
    }

    private static async Task<bool> Test_Post_Cat_Valid_Id_No_Photo()
    {
        _playgroundServer.ClearRequestResponses();

        ServerConfig.StorageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");

        // Route 3rd-party API client to the playground server
        PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
        PhotoClient.Instance.Port = ServerConfig.Port;

        CatManager catCaller = new();
        HttpStatusCode result = await catCaller.CreateCatWithPhoto(1, "No Photo").ConfigureAwait(false);

        if (result != HttpStatusCode.Created) return false;

        // Verify call sequence (POST -> GET)
        return (_playgroundServer.RequestResponses.Count == 2 &&
            ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
            ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/1"));
    }

    private static async Task<bool> Test_Post_Cat_Valid_Id_And_Photo()
    {
        _playgroundServer.ClearRequestResponses();

        ServerConfig.StorageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData/Functional");

        PhotoClient.Instance.BaseUrl = ServerConfig.HostName;
        PhotoClient.Instance.Port = ServerConfig.Port;

        CatManager catCaller = new();
        HttpStatusCode result = await catCaller.CreateCatWithPhoto(3, "With Photo").ConfigureAwait(false);

        if (result != HttpStatusCode.OK) return false;

        // Verify call sequence (POST -> GET -> PATCH)
        return (_playgroundServer.RequestResponses.Count == 3 &&
            ValidateRequest(_playgroundServer.RequestResponses[0].Item1, HttpMethod.Post, RequestSender.RestPath) &&
            ValidateRequest(_playgroundServer.RequestResponses[1].Item1, HttpMethod.Get, PhotoClient.RelativeURL + "/3") &&
            ValidateRequest(_playgroundServer.RequestResponses[2].Item1, HttpMethod.Patch, RequestSender.RestPath));
    }

    private static bool ValidateRequest(RequestModel req, HttpMethod verb, string urlEnd)
        => req.Verb == verb.Method && req.URL.EndsWith(urlEnd);
}
```

---

## Server simulation: custom logic + file-based responses

HttpPlaygroundServer supports server simulation by letting you plug in a request processor. The example below shows a **multi-server simulator** that routes requests to either:

- **Cat API simulation** (file-based responses via `RetrieveByFile(...)`)
- **Photo API simulation** (simple in-memory logic)

### MultiServerSimulator (excerpt)

```csharp
internal class MultiServerSimulator : HttpRequestProcessor
{
    protected override Task<ResponseModel> SimulateServerHandling(RequestModel rs)
    {
        // Redirect for cat or photo based on URL content
        if (rs.URL.Contains("cat"))
        {
            return RetreiveCatResponse(rs);
        }

        return RetreivePhotoResponse(rs);
    }

    private Task<ResponseModel> RetreivePhotoResponse(RequestModel rs)
    {
        ResponseModel rm = new();
        rm.Headers.Add("Content-Type", "plain/text; charset=utf-8");

        int catIdInd = rs.URL.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1;
        string catId = rs.URL.Substring(catIdInd);

        if (catId == "-1")
        {
            rm.Body = "Not valid";
            rm.StatusCode = HttpStatusCode.BadRequest;
        }
        else if (catId == "3")
        {
            rm.Body = $"https://yourcatphoto/Cat{catId}";
            rm.StatusCode = HttpStatusCode.OK;
        }
        else
        {
            rm.StatusCode = HttpStatusCode.NotFound;
            rm.Body = null;
        }

        var ret = new Task<ResponseModel>(() => rm);
        ret.RunSynchronously();
        return ret;
    }

    private Task<ResponseModel> RetreiveCatResponse(RequestModel rs)
    {
        if (rs.Verb == HttpMethod.Get.Method)
        {
            int catEndInd = rs.URL.IndexOf("cats", StringComparison.InvariantCultureIgnoreCase) + 4;

            if (rs.URL.Contains("-1"))
            {
                return base.RetrieveByFile("404.json", rs.URL.Remove(catEndInd));
            }

            return base.RetrieveByFile("CatGet.json", rs.URL.Remove(catEndInd));
        }
        else if (rs.Verb == HttpMethod.Post.Method)
        {
            CatModel? catModel = JsonSerializer.Deserialize<CatModel?>(rs.Body);
            string responseFile = (catModel?.Id == -1) ? "400.json" : "CatPost.json";
            return base.RetrieveByFile(responseFile);
        }
        else
        {
            return base.RetrieveByFile("Patch.json");
        }
    }
}
```

**Why this matters for functional testing:** you can simulate realistic backend behaviors (including branching) while still keeping the system local and deterministic.

---

## What to verify in functional tests

HttpPlaygroundServer **records** request–response pairs; your harness verifies behavior.

Typical checks:
- **Sequence:** POST → GET (photo missing), POST → GET → PATCH (photo available)
- **Count:** expected number of calls
- **Targets:** correct endpoint path suffixes
- **Verb correctness:** POST/GET/PATCH as expected

---

## Notes

- Use `ClearRequestResponses()` before each workflow run.
- You can enable/disable disk logging via `IsRequestLoggingEnabled`.
- `ServerConfig.StorageFolder` controls where file-based responses and logs live.

---

## Source and samples

GitHub repository (source + console samples):
https://github.com/sameerkapps/HttpPlayground
