////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;
using System.Net;
using System.Threading.Tasks;

using HttpPlaygroundServer.Model;

namespace HttpPlaygroundServer
{
    internal class HttpRequestProcessor
    {
        StorageManager _storeManager = new StorageManager();
        /// <summary>
        /// Processes an incoming HTTP request and generates an appropriate response.
        /// </summary>
        /// <remarks>This method handles the HTTP request by storing its details asynchronously, and then
        /// sends a response  based on the request's HTTP method and URL. If an error occurs during processing, the
        /// response status  code is set to 500 (Internal Server Error), and the exception message is written to the
        /// response body.</remarks>
        /// <param name="context">The <see cref="HttpListenerContext"/> containing the HTTP request and response objects.</param>
        /// <returns></returns>
        internal async Task ProcessRequests(HttpListenerContext context)
        {
            // Retrieve the HTTP request from the context
            HttpListenerRequest request = context.Request;

            try
            {
                // Store the HTTP request details asynchronously
                await StoreRequest(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If an exception occurs, set the response status code to 500 (Internal Server Error)
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // Write the exception message to the response body
                await WriteResponseBody(context.Response, ex.Message).ConfigureAwait(false);

                // Exit the method to prevent further processing
                return;
            }

            // Send an HTTP response based on the request's HTTP method and URL
            await SendResponse(request.HttpMethod, request.Url, context.Response).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously stores the details of an HTTP request.
        /// </summary>
        /// <remarks>This method parses the provided HTTP request and stores its details using the
        /// underlying storage manager. Ensure that the <paramref name="request"/> parameter is valid and properly
        /// initialized before calling this method.</remarks>
        /// <param name="request">The <see cref="HttpListenerRequest"/> containing the HTTP request data to be stored. This parameter cannot
        /// be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task StoreRequest(HttpListenerRequest request)
        {
            // Parse the result
            RequestStorage rs = HttpRequestParser.Parse(request);

            // Write the request
            await _storeManager.StoreRequest(rs).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an HTTP response based on the specified HTTP method and URL.
        /// </summary>
        /// <remarks>This method retrieves the response data from a storage manager based on the provided
        /// HTTP method and URL. It sets the HTTP status code, headers, and body of the response. If an error occurs
        /// during processing, the response status code is set to 500 (Internal Server Error), and the response body
        /// contains the error message.</remarks>
        /// <param name="httpMethod">The HTTP method of the request (e.g., GET, POST).</param>
        /// <param name="url">The URL of the request, used to retrieve the corresponding response data.</param>
        /// <param name="response">The <see cref="HttpListenerResponse"/> object to which the response will be written.</param>
        /// <returns></returns>
        private async Task SendResponse(string httpMethod, Uri url, HttpListenerResponse response)
        {
            // Initialize the response body as an empty string
            string strBody = string.Empty;
            try
            {
                // Retrieve the response data from the storage manager based on the HTTP method and URL
                ResponseStorage rs = await _storeManager.RetrieveResponse(httpMethod, url.AbsoluteUri).ConfigureAwait(false);

                // Set the HTTP status code for the response
                if (Enum.IsDefined(typeof(HttpStatusCode), rs.StatusCode))
                {
                    response.StatusCode = (int)rs.StatusCode;
                }
                else
                {
                    throw new Exception($"Invalid StatusCode {rs.StatusCode} in json");
                }

                // Add headers to the response if they exist
                bool isJson = true;
                if (rs.Headers != null)
                    foreach (var kvp in rs.Headers)
                    {
                        response.Headers[kvp.Key] = kvp.Value;
                        if(kvp.Key == "Content-Type" && 
                            !kvp.Value.StartsWith("application/json"))
                        {
                            isJson = false;
                        }
                    }

                // Convert the response body to a JSON string if it exists
                if (isJson)
                {
                    strBody = rs.Body?.ToJsonString();
                }
                else
                {
                    strBody = rs.Body?.ToString();
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, set the status code to 500 (Internal Server Error)
                response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // Set the response body to the exception message
                strBody = ex.Message;
            }

            // Write the response body to the output stream
            await WriteResponseBody(response, strBody).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the response body to the output stream of the HttpListenerResponse.
        /// </summary>
        /// <param name="response">The HttpListenerResponse object to write the body to.</param>
        /// <param name="strBody">The string content to write as the response body.</param>
        private static async Task WriteResponseBody(HttpListenerResponse response, string strBody)
        {
            // Check if the response body is not null or empty
            if (!string.IsNullOrEmpty(strBody))
            {
                // Convert the string body to a byte array using UTF-8 encoding
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strBody);
                // Set the ContentLength64 property to the length of the byte array
                response.ContentLength64 = buffer.Length;
                // Write the byte array to the response's output stream asynchronously
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }

            // Close the output stream to complete the response
            response.OutputStream.Close();

            // Close the HttpListenerResponse to release resources
            response.Close();
        }
    }
}
