
---

# Http Playground Server

A lightweight local HTTP server for testing, debugging, and inspecting HTTP requests.
It logs all incoming requests (URL, headers, body) and returns mock responses from predefined files, allowing you to simulate various scenarios without the overhead of running a full web service.

---

## Features

* 🚀 Run a lightweight HTTP test server locally
* 📜 Automatically logs request URL, headers, and body
* 📁 Stores each request in a timestamped JSON file
* 🔁 Returns mock responses based on files in a *Responses* folder
* 🎛️ Supports query-based response selection (`?respFile=`)
* 🧪 Simulates various HTTP results for testing and automation

---

# How It Works

When the server receives an HTTP request:

1. It creates (if needed) a folder structure based on the URL path.
2. The request is logged into the **Requests** subfolder with a timestamped JSON file.
3. The server looks for a mock response file in the **Responses** subfolder.
4. If found, the file is used to construct the response.
5. If not found, a default success response is returned.

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

### 4. Start the Server

```csharp
// cancellation token for the server loop
CancellationTokenSource cts = new CancellationTokenSource();

// this is signaled when the server starts
ManualResetEventSlim serverStarted = new();

// Update server storage folder
string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
ServerConfig.StorageFolder = storageFolder;

Console.WriteLine($"Storage Folder is {ServerConfig.StorageFolder}");

// instantiate and run the server
HttpPlaygoundServer httpTestListener = new();
Task httpListener = Task.Run(() => httpTestListener.StartHttpListner(serverStarted, cts.Token));

// wait for server to start
serverStarted.Wait();
```

---

## Sample Code

Sample code demonstrating how to set up and use the Http Playground Server is available in this repository:

🔗 **GitHub:** https://github.com/sameerkapps/HttpPlayground

The sample project includes:

- How to configure the `ServerConfig` options  
- How to start the server loop with proper async/await usage  
- How to organize your `Requests` and `Responses` folders  
- Example mock response files  
- A minimal console app you can copy and adapt for your own tests
- A Test app with a wide range of scenarios

This sample is the quickest way to understand the expected folder structure, response file format, and overall workflow.


---

# Request Logging

When a request is received, the server ensures that the folder structure based on the request URI exists or is created.

e.g. `GET /Pets/Cats` request is saved in:

```
/Pets/Cats/Requests/GET-20251109-130233.340.json
```

### Request File Format

```json
{
    "Uri": "uri of the request",
    "Headers": {
        "key": "value"
    },
    "Body": {
        // JSON or body received
    }
}
```

---

# Mock Responses

To respond, the server checks for a response file in the **Responses** folder.

Example for:

```
GET http://localhost:8080/Pets/Cats
```

It looks for:

```
d:/temp/Pets/Cats/Responses/Get.json
```

If the file is missing, it will return following HttpStatusCode:

* Default: `200 OK` (or `201` for POST, `204` for DELETE`)

### Query-Based Response Selection

If the request contains:

```
?respFile=GetAll.json
```

Then the server will look for:

```
d:/temp/Pets/Cats/Responses/GetAll.json
```

If `respFile` is specified and missing → it will return `404 Not Found`

### Response File Format

```json
{
    "HttpStatusCode": "desired status code",
    "Headers": {
        "key": "value"
    },
    "Body": {
        // JSON or text
    }
}
```

---

# Configuration

Default values exist for the port, hostname, and storage folder.
You can override them by setting values on the `ServerConfig` object:

```csharp
ServerConfig.Port = 8080;
ServerConfig.HostName = "localhost";
ServerConfig.StorageFolder = "d:/temp";
```
