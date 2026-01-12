
---

# Http Playground Server

This facilitates functional testing on local machine. It provides minimalist API with flixibility for customization. It provides a lightweight local HTTP server to accept requests, send response and provides ability to validate the workflow of a function. It also provides simulation of server logic and logging of the request (URL, headers, body).
It allows you to simulate various scenarios without the overhead of running a full web service or third party APIs

---

## Features

* 🚀 Run a lightweight HTTP test server locally
* 🚀 Provides collection of request responses for validation
* 🚀 Provides 
* 📜 Automatically logs request URL, headers, and body
* 📁 Stores each request in a timestamped JSON file
* 🔁 Returns mock responses based on files in a *Responses* folder
* 🧪 Simulates various HTTP results for testing and automation

---

# What's new in 2.0
* `HttpPlaygroundServer` has a new property `RequestResponses`
This list exposes the sequenence of requests and responses in the order of the reqeusts sent. This can be used to validate the workflow.

* `HttpPlaygroundServer` has a new method `ClearRequestResponses`
This will clear the Requests and responses that are stored

* `StartHttpListner` is renamed to `StartServer`
This is to provide clarity.

* `HttpPlaygroundServer` has a new method `StopServer`
This will stop the server and clean up ports.

* `HttpPlaygroundServer` has a new property `IsRequestLoggingEnabled`
This determines if the requests should be logged to disk or not. It is `true` by default.

# Breaking Changes (from 1.0)

* Query-based response selection (`?respFile=`) has been removed.
New version provides server based simulation. So this parameter is now redundant.
* StartHttpListner has been renamed to StartServer
This is to provide clarity.

# How it works

When the server receives an HTTP request:

1. The request is logged to disk based on the value of `IsRequestLoggingEnabled`
 If needed, it creates a folder structure based on the URL path.
2. The request is stored in `RequestResponses` property for workflow validation.
3. Then it calls virtual method `SimulateServerHandling` to process the request.
By default this method will return response based on the URL and Verb. A class inherited from `HttpRequestProcessor` can override thsi method to simulate server behavior and provide response based on another file or built by code.
4. The returned response in updated in the `RequestResponses` collection.

## Default behavior

1. When request is received, the server looks for a response file corresponding to verb and URL path in the **Responses** subfolder.
2. If found, the file is used to construct the response.
3. If not found, a default success response is returned.

**Example folder layout for `d:/temp` and request to `/Pets/Cats`:**

```
d:/temp/Pets/Cats/
    Requests/
    Responses/
```

---

# How to Use

### 1. Create a Console App

### 2. Install the NuGet Package

```
sameerk.HttpPlaygroundServer
```

### 3. Ensure Main is `async`

```csharp
static async Task Main(string[] args)
```

### 4. Start the Server and wait for it to start

```csharp
            CancellationTokenSource cts = new CancellationTokenSource();
            // Server will trigger this after starting
            ManualResetEventSlim serverStarted = new();
            // disable request logging
            _playgroundServer.IsRequestLoggingEnabled = false;

            // run it in a different task/thread
            _ = Task.Run(async () => { await _playgroundServer.StartServer(serverStarted, cts.Token).ConfigureAwait(false); });

            // wait for the server to start
            serverStarted.Wait();
```

5. Testing goes here

```CSharp
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

```

6. Shutdown the server

```CSharp
            // cancel the server listening operation
            cts.Cancel();

            // Cleanup
            _playgroundServer.StopServer();
```

---

## Sample Code

Sample code demonstrating how to set up and use the Http Playground Server is available in this repository:

🔗 **GitHub:** https://github.com/sameerkapps/HttpPlayground

The sample project includes:

* How to perform functional testing
* How to simulate Server logic
* How to configure the `ServerConfig` options  
* How to organize your `Requests` and `Responses` folders
* Example mock response files  
* A minimal console app you can copy and adapt for your own tests
* A Test app with a wide range of scenarios
