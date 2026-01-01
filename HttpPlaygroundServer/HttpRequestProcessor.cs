////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

using HttpPlaygroundServer.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HttpPlaygroundServer
{
    public class HttpRequestProcessor
    {
        /// <summary>
        /// Logs requests by default.
        /// </summary>
        internal bool LogRequests { get; set; } = true;

        // _listReqResp
        object _lockObj = new object();

        List<Tuple<RequestModel, ResponseModel>> _listReqResp = new List<Tuple<RequestModel, ResponseModel>>();

        internal IReadOnlyList<Tuple<RequestModel, ResponseModel>> RequestResponses => _listReqResp;

        internal void ClearRequestResponses()
        {
            lock(_lockObj)
            {
                _listReqResp.Clear();
            }
        }

        private StorageManager _currentStorage = null;
        /// <summary>
        /// Processes an incoming HTTP request and generates an appropriate response.
        /// </summary>
        /// <remarks>This method handles the HTTP request by storing its details asynchronously, and then
        /// sends a response  based on the request's HTTP method and URL. If an error occurs during processing, the
        /// response status  code is set to 500 (Internal Server Error), and the exception message is written to the
        /// response body.</remarks>
        /// <param name="context">The <see cref="HttpListenerContext"/> containing the HTTP request and response objects.</param>
        /// <returns></returns>
        internal protected virtual async Task ProcessRequests(HttpListenerContext context)
        {
            // Create a new store manager for each new request
            _currentStorage = new StorageManager();
            // Retrieve the HTTP request from the context
            HttpListenerRequest request = context.Request;

            RequestModel requestModel = default;
            ResponseModel responseModel;

            try
            {
                // Parse the request
                requestModel = HttpRequestParser.Parse(request);

                // Store the HTTP request details asynchronously
                if (LogRequests)
                {
                    await _currentStorage.StoreRequest(requestModel).ConfigureAwait(false);
                }
                else
                {
                    _currentStorage.CreateFoldersFromUrl(requestModel.URL, false);
                }

                // Send an HTTP response based on the request's HTTP method and URL
                responseModel = await SendResponse(requestModel, context.Response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if(requestModel == null)
                {
                    requestModel = new RequestModel()
                    {
                        // Store the URL of the request.
                        URL = request.Url.AbsoluteUri,

                        // Store the HTTP method (e.g., GET, POST) of the request.
                        Verb = request.HttpMethod,
                        Body = ex.Message
                    };
                }

                // If an exception occurs, set the response status code to 500 (Internal Server Error)
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // Write the exception message to the response body
                await WriteResponseBody(context.Response, ex.Message).ConfigureAwait(false);

                responseModel = new ResponseModel()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Headers = new Dictionary<string, string>(),
                    Body = ex.Message
                };
            }

            AddReqResp(requestModel, responseModel);
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
        protected virtual async Task<ResponseModel> SendResponse(RequestModel requestData, HttpListenerResponse response)
        {
            string responseBody = string.Empty;
            ResponseModel responseData;
            try
            {
                responseData = await SimulateServerHandling(requestData).ConfigureAwait(false);

                SetStatusCode(response, responseData.StatusCode);
                ApplyHeaders(response, responseData.Headers);

                responseBody = FormatResponseBody(responseData.Body, response);
            }
            catch (Exception ex)
            {
                HandleResponseError(response, ex, out responseBody);

                responseData = new ResponseModel()
                                    {
                                        Headers = new Dictionary<string, string>(),
                                        StatusCode = HttpStatusCode.InternalServerError,
                                        Body = ex.Message
                                    };
            }

            await WriteResponseBody(response, responseBody).ConfigureAwait(false);

            return responseData;
        }

        protected virtual async Task<ResponseModel> SimulateServerHandling(RequestModel rs)
        {
            // Retrieve the response data from the storage manager based on the HTTP method
            return await RetrieveByVerb(rs).ConfigureAwait(false);
        }

        protected virtual Task<ResponseModel> RetrieveByVerb(RequestModel rs)
        {
            // Retrieve the response data from the storage manager based on the HTTP method
            return _currentStorage.RetrieveResponseByVerb(rs.Verb);
        }

        protected virtual Task<ResponseModel> RetrieveByFile(string filename, string url= null)
        {
            // Retrieve the response data from the storage manager based on the file name
            return _currentStorage.RetrieveResponseByFileName(filename, url);
        }

        private void SetStatusCode(HttpListenerResponse response, HttpStatusCode statusCode)
        {
            if (!Enum.IsDefined(typeof(HttpStatusCode), statusCode))
            {
                throw new InvalidOperationException($"Invalid StatusCode '{statusCode}' in response data");
            }

            response.StatusCode = (int)statusCode;
        }

        private void ApplyHeaders(HttpListenerResponse response, IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                response.Headers[header.Key] = header.Value;
            }
        }

        private string FormatResponseBody(JsonNode body, HttpListenerResponse response)
        {
            if (body == null)
            {
                return string.Empty;
            }

            bool isJsonResponse = IsJsonContentType(response);

            return isJsonResponse
                ? body.ToJsonString()
                : body.ToString();
        }

        private bool IsJsonContentType(HttpListenerResponse response)
        {
            string contentType = response.Headers["Content-Type"];

            if (string.IsNullOrEmpty(contentType))
            {
                return true; // Default to JSON if no Content-Type is specified
            }

            return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private void HandleResponseError(HttpListenerResponse response, Exception ex, out string errorBody)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            errorBody = ex.Message;
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

        /// <summary>
        /// Add one at a time. Lock is not efficient; queuing should be used for performance.
        /// But this is not related to measuring performance.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        private void AddReqResp(RequestModel request, ResponseModel response)
        {
            lock(_lockObj)
            {
                _listReqResp.Add(new Tuple<RequestModel, ResponseModel>(request, response));
            }
        }
    }
}
