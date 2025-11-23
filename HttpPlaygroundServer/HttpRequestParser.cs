////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Net;
using System.Text.Json.Nodes;

using HttpPlaygroundServer.Model;

namespace HttpPlaygroundServer
{
    /// <summary>
    /// Provides functionality to parse HTTP requests into a structured format.
    /// </summary>
    /// <remarks>This class is designed to process instances of <see cref="HttpListenerRequest"/> and convert
    /// them into <see cref="RequestStorage"/> objects, which encapsulate the request's URL, HTTP method, headers, and
    /// body. The body is parsed as JSON if possible; otherwise, it is stored as a raw string.</remarks>
    public class HttpRequestParser
    {
        /// <summary>
        /// Parses an HttpListenerRequest into a RequestStorage object.
        /// </summary>
        /// <param name="request">The HttpListenerRequest to parse.</param>
        /// <returns>A RequestStorage object containing the parsed data.</returns>
        public static RequestStorage Parse(HttpListenerRequest request)
        {
            // Initialize a new RequestStorage object to store the parsed request data.
            RequestStorage rs = new RequestStorage();

            // Store the URL of the request.
            rs.URL = request.Url.AbsoluteUri;

            // Store the HTTP method (e.g., GET, POST) of the request.
            rs.Verb = request.HttpMethod;

            // Iterate through all headers in the request and add them to the RequestStorage object.
            foreach (string key in request.Headers.AllKeys)
            {
                rs.Headers.Add(key, request.Headers[key]);
            }

            // Check if the request contains a body (InputStream is not null).
            if (request.InputStream != null)
            {
                // Read the body of the request using a StreamReader.
                using (StreamReader reader = new StreamReader(request.InputStream))
                {
                    string body = reader.ReadToEnd();

                    // If the body is not empty, attempt to parse it as JSON.
                    if (!string.IsNullOrEmpty(body))
                    {
                        try
                        {
                            rs.Body = JsonNode.Parse(body); // Parse the body as a JSON object.
                        }
                        catch (Exception ex)
                        {
                            // If parsing fails, store the raw body and the exception message.
                            rs.Body = $"Body: {body} Exception {ex.Message}";
                        }
                    }
                    else
                    {
                        // If the body is empty, store an empty string.
                        rs.Body = string.Empty;
                    }
                }
            }
            else
            {
                // If there is no body, store an empty string.
                rs.Body = string.Empty;
            }

            // Return the populated RequestStorage object.
            return rs;
        }
    }
}
