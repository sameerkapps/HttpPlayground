////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Nodes;

namespace HttpPlaygroundServer.Model
{
    /// <summary>
    /// Represents a storage model for HTTP responses, including status code, headers, and body content.
    /// </summary>
    public class ResponseModel
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the headers of the HTTP response as a dictionary of key-value pairs.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the body of the HTTP response as a JSON node.
        /// </summary>
        public JsonNode Body { get; set; }
    }
}
