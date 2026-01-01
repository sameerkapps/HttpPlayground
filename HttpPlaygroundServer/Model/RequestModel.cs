////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HttpPlaygroundServer.Model
{
    /// <summary>
    /// Represents the storage for an HTTP request, including its URL, HTTP verb, headers, and body.
    /// </summary>
    /// <remarks>This class provides a structure for storing the components of an HTTP request. It includes
    /// the URL,  HTTP verb (e.g., GET, POST), headers as key-value pairs, and the body as a JSON object.</remarks>
    public class RequestModel
    {
        /// <summary>
        /// The URL of the HTTP request.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// The HTTP verb (e.g., GET, POST, PUT, DELETE) used in the request.
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// A dictionary to store the headers of the HTTP request, where the key is the header name and the value is the header value.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The body of the HTTP request, represented as a JsonNode for flexible JSON handling.
        /// </summary>
        public JsonNode Body { get; set; }
    }
}
