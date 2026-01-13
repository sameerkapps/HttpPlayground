# Overview

This repository contains two solutions and five projects that together demonstrate how to use, test, and develop the **HttpPlaygroundServer** NuGet package.  
If you're looking for a pakcage for Functional testing while simulating server logic and logging requests, you have come to the righ repository. This repository provides both the package source and example usage.

---

# Prerequisites

Before running or building the projects, ensure you have:

- **.NET 8 SDK** (required for all projects)  
- An IDE such as:
  - Visual Studio 2022  
  - Visual Studio Code  

---

# Repository Structure

```

/
├── PlaygroundServerNuGetSample.sln
│   ├── FunctionalTestingApp
│   ├── PlaygroundServerApp
│   └── PlaygroundSampleApp   
│
└── HttpPlaygroundServer.sln
├── HttpPlaygroundServer
└── HttpPlaygroundTestApp

```

---

# Solution 1: PlaygroundServerNuGetSample.sln

This solution demonstrates how to **consume and use** the `HttpPlaygroundServer` NuGet package.

## Projects

### FunctionalTestingApp
A console app the hosts the Http Playground Server using the NuGet package and also demonstrates how to run functional tests using it.

### PlaygroundServerApp
A console app that hosts the Http Playground Server using the NuGet package.

### PlaygroundSampleApp
A console app that sends sample HTTP requests to the server after it starts.

---

## Running the Functional Testing Sample

1. Open **PlaygroundServerNuGetSample.sln**.  
2. Select `FunctionalTestingApp` as startup project.
3. Run the solution. 
It will display the result of the functional testing. You can inspect the code to see how it work.

## Running the other Sample
This demos how server can be run independently in an app. With that you can use it to test apps from other languages.

To see the functionality of both server and the caller, both applications must run **simultaneously**.

### Steps

1. Open **PlaygroundServerNuGetSample.sln**.  
2. Set **both** projects (`PlaygroundServerApp` and `PlaygroundSampleApp`) to start together.  
3. Run the solution.  
4. Wait for the server to finish initializing. The `PlaygroundServerApp` console will show:

```

Server has started listening. You may begin requests...

```

5. Switch to the `PlaygroundSampleApp` console. It will show:

```

Wait till the server starts. Press Enter when the server is ready to accept requests.

```

6. Press **Enter** to send the sample HTTP requests to the server.

7. Inspect the logged requests here:

```

PlaygroundSampleApp/bin/Debug/net8.0/TestData/Pets/Cats/Requests

```

8. To explore more scenarios (different verbs, missing response files, error cases), refer to **HttpPlaygroundTestApp** in the second solution.

---

# Request/Response Flow Diagram

Below is a simplified diagram showing how the PlaygroundServer and SampleApp interact:

```

┌────────────────────────┐        HTTP Request         ┌────────────────────────┐
│   PlaygroundSampleApp   │ ──────────────────────────► │  PlaygroundServerApp   │
└────────────────────────┘                             └────────────────────────┘
│                                                       │
│            Response (mock or default)                 │
◄───────────────────────────────────────────────────────┘
│
│ Logs written to:
▼
PlaygroundSampleApp/bin/Debug/net8.0/TestData/...

```

---

# Solution 2: HttpPlaygroundServer.sln

This solution is intended for **development**, **debugging**, and **building the NuGet package**.

## Projects

### HttpPlaygroundServer
The core project that produces the `HttpPlaygroundServer` NuGet package.  
This contains all server logic, request logging, response file resolution, and configuration.

### HttpPlaygroundTestApp
A console application for manually testing different server behaviors, including:

- Server simulation
- Functional validation using workflow
- All major HTTP verbs  
- Requests with and without response files  
- Selecting custom response files via the `respFile` query parameter  
- Error-handling scenarios  
- Various combinations useful for automated testing  

Use this app as a reference for building your own functional and other tests.

---

# Additional Resources

### 📦 NuGet Package README
To learn more about how the package works internally, see the README included with the NuGet project:

```

HttpPlaygroundServer/README.md

```

You may also visit the NuGet package page [sameerk.HttpPlaygroundServer](https://www.nuget.org/packages/sameerk.HttpPlaygroundServer/).

### 📘 Main Package Documentation
For API-level details, configuration options, and examples, refer to the primary README in the package source.

---

# Support

If you have questions or suggestions, feel free to open an issue. I do not have enough bandwidth to maintain this project. So there will be few updates.


