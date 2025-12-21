////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;

namespace PlaygorundTestApp
{
    internal class RequestSender
    {
        /// <summary>
        /// Defines the supported methods for sending data to an API.
        /// </summary>
        private enum SendDataMethods
        {
            /// <summary>
            /// POST: Used to create a new resource on the server.
            /// </summary>
            Post,

            /// <summary>
            /// PUT: Used to update or replace an existing resource entirely.
            /// </summary>
            Put,

            /// <summary>
            /// PATCH: Used to apply partial modifications to an existing resource.
            /// </summary>
            Patch
        }

        internal const string RestPath = "pets/cats";

        /// <summary>
        /// This makes Get Request and returns StatusCode, headers, and body
        /// </summary>
        /// <param name="path"></param>
        /// <param name="respFile"></param>
        /// <returns></returns>
        internal async Task<(HttpStatusCode, Dictionary<string, string> headers, string)> SendGet(string path = "")
        {
            using var httpClient = new HttpClient();
            try
            {
                UriBuilder uriBuilder = new UriBuilder("http", ServerConfig.HostName, ServerConfig.Port, RestPath + path);

                HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri.AbsoluteUri);

                Dictionary<string, string> headers = new();
                foreach(var header in response.Headers)
                {
                    headers[header.Key] = header.Value.FirstOrDefault();
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return (response.StatusCode, headers, responseBody);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

                return ((HttpStatusCode)ex.HResult, null, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

                return (HttpStatusCode.InternalServerError, null, ex.Message);
            }
        }

        /// <summary>
        /// This sends delete request
        /// </summary>
        /// <param name="path"></param>
        /// <param name="respFile"></param>
        /// <returns></returns>
        internal async Task<(HttpStatusCode, string)> SendDelete(string path = "")
        {
            using var httpClient = new HttpClient();
            try
            {
                UriBuilder uriBuilder = new UriBuilder("http", ServerConfig.HostName, ServerConfig.Port, RestPath, path);

                HttpResponseMessage response = await httpClient.DeleteAsync(uriBuilder.Uri.AbsoluteUri);

                string responseBody = await response.Content.ReadAsStringAsync();

                return (response.StatusCode, responseBody);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

                return ((HttpStatusCode)ex.HResult, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

                return (HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        
        internal async Task<(HttpStatusCode, string)> SendPost()
        {
            return await SendData(SendDataMethods.Post);
        }

        internal async Task<(HttpStatusCode, string)> SendPut()
        {
            return await SendData(SendDataMethods.Put);
        }

        internal async Task<(HttpStatusCode, string)> SendPatch()
        {
            return await SendData(SendDataMethods.Patch);
        }

        private async Task<(HttpStatusCode,string)> SendData(SendDataMethods method)
        {
            using var httpClient = new HttpClient();

            // Create the object to send
            var payload = new CatModel
            {
                Id = 99,
                Name = "Blue",
            };

            // Serialize to JSON
            string json = JsonSerializer.Serialize(payload);

            // Create StringContent with JSON
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                UriBuilder uriBuilder = new UriBuilder("http", ServerConfig.HostName, ServerConfig.Port);
                uriBuilder.Path += RestPath;

                HttpResponseMessage response = null;

                if (method == SendDataMethods.Post)
                {
                    response = await httpClient.PostAsync(uriBuilder.Uri.AbsoluteUri, content).ConfigureAwait(false);
                }
                else if (method == SendDataMethods.Put)
                {
                    response = await httpClient.PutAsync(uriBuilder.Uri.AbsoluteUri, content).ConfigureAwait(false);
                }
                else if (method == SendDataMethods.Patch)
                {
                    response = await httpClient.PatchAsync(uriBuilder.Uri.AbsoluteUri, content).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidEnumArgumentException(nameof(method));
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return (response.StatusCode, responseBody);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

                return ((HttpStatusCode)ex.HResult, ex.Message);
            }
            catch (Exception ex){
                Console.WriteLine($"Request error: {ex.Message}");

                return (HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
