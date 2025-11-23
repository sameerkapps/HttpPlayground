////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using PlaygorundTestApp;
using System.Text;
using System.Text.Json;

namespace PlaygroundSampleApp;

/// <summary>
/// This is a quick sample app for how to use Playground server.
/// It sends one Post request with expecting response for Post
/// It sends one Get request expecting the response defines in the "Server's" TestData/Pets/Cats/Responses/Get.json file
/// 
/// For other scenarios such as customized response file. non-json requests, please check PlaygroundTestApp.
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Wait till the server starts. Press Enter when the server is ready to accept requests.");
        Console.ReadLine();

        await MakeHttpRequests().ConfigureAwait(false);

        Console.WriteLine("Sent all the requests.");
        Console.WriteLine("Press Enter to end...");
        Console.ReadLine();
    }

    private static async Task MakeHttpRequests()
    {
        using var httpClient = new HttpClient();

        try
        {
            UriBuilder uriBuilder = new UriBuilder("http", "localhost", 8080);
            uriBuilder.Path += "pets/cats";

            await PostCatData(httpClient, uriBuilder).ConfigureAwait(false);

            await GetCatData(httpClient, uriBuilder).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
        }

    }

    private static async Task PostCatData(HttpClient httpClient, UriBuilder uriBuilder)
    {
        // Create the object to send
        var payload = new CatModel
        {
            Id = 007,
            Name = "Rock"
        };

        // Serialize to JSON
        string json = JsonSerializer.Serialize(payload);

        // Create StringContent with JSON
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync(uriBuilder.Uri.AbsoluteUri, content).ConfigureAwait(false);

        Console.WriteLine("---- Begin POST ----");
        Console.WriteLine($"StatusCode: {response.StatusCode}");
        Console.WriteLine($"Headers Count: {response.Headers.Count()}");
        
        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        Console.WriteLine($"Post Body:");
        Console.WriteLine(responseBody);
        Console.WriteLine("---- End POST ----");
    }

    private static async Task GetCatData(HttpClient httpClient, UriBuilder uriBuilder)
    {
        HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri.AbsoluteUri).ConfigureAwait(false);

        Console.WriteLine("---- Begin GET ----");
        Console.WriteLine($"StatusCode: {response.StatusCode}");
        Console.WriteLine($"Headers Count: {response.Headers.Count()}");
        
        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        Console.WriteLine($"Get Body:");
        Console.WriteLine(responseBody);
        Console.WriteLine("---- End GET ----");
    }
}
