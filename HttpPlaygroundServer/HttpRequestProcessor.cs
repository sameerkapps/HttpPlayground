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

        // lock for _listReqResp
        object _lockObj = new object();

        // field to store Request Response pairs
        List<Tuple<RequestModel, ResponseModel>> _listReqResp = new List<Tuple<RequestModel, ResponseModel>>();

        /// <summary>
        /// Property returns Request Response pairs as Read only list
        /// </summary>
        internal IReadOnlyList<Tuple<RequestModel, ResponseModel>> RequestResponses => _listReqResp;

        /// <summary>
        /// Clears all Request responses
        /// </summary>
        internal void ClearRequestResponses()
        {
            lock(_lockObj)
            {
                _listReqResp.Clear();
            }
        }

        private StorageManager _currentStorage = null;
        /// <summary>
        /// Processes an incoming HTTP request and generates an appropriate response. The request response pair is stored to facilitate history checking.
        /// </summary>
        /// <remarks>This method handles the HTTP request by optinally storing its details asynchronously, and then
        /// sends a response  based on the request's HTTP method and URL. If an error occurs during processing, the
        /// response status  code is set to 500 (Internal Server Error), and the exception message is written to the
        /// response body.</remarks>
        /// <param name="context">The <see cref="HttpListenerContext"/> containing the HTTP request and response objects.</param>
        /// <returns></returns>
        internal protected virtual async Task ProcessRequests(HttpListenerContext context)
        {
            int reqIndex = 0;
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

                // Store the HTTP request details optionally
                if (LogRequests)
                {
                    await _currentStorage.SaveRequest(requestModel).ConfigureAwait(false);
                }
                else
                {
                    // if request is not stored, folders are updated for response.
                    _currentStorage.CreateFoldersFromUrl(requestModel.URL, false);
                }

                // Send an HTTP response based on the request's HTTP method and URL
                reqIndex = SaveRequest(requestModel);
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

                reqIndex = SaveRequest(requestModel);

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

            // store response for the request
            AddReqResp(reqIndex, responseModel);
        }

        /// <summary>
        /// Sends an HTTP response based on the request. Thsi can be customized to simulate server side handling.
        /// By default, it provides response based on the HTTP method and files stored in the folder corresponding to the URL.
        /// </summary>
        /// <remarks>This method retrieves the response data using virtual method SimulateServerHandling. 
        /// Based on that it sets the HTTP status code, headers, and body of the response. If an error occurs
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
                // get the response data
                responseData = await SimulateServerHandling(requestData).ConfigureAwait(false);

                // update status and headers of the response
                SetStatusCode(response, responseData.StatusCode);
                ApplyHeaders(response, responseData.Headers);

                // update body of the response
                responseBody = FormatResponseBody(responseData.Body, response);
            }
            catch (Exception ex)
            {
                // if there is an exception
                // write response with 500
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

        /// <summary>
        /// This will simulate server processing. By default it retrieves response based on the verb.
        /// To customize the bhavior, you can override this method and build response from scratch or
        /// Build response from a file and optionally modify it.
        /// </summary>
        /// <param name="rs">request model</param>
        /// <returns>Response model</returns>
        protected virtual async Task<ResponseModel> SimulateServerHandling(RequestModel rs)
        {
            // Retrieve the response data from the storage manager based on the HTTP method
            return await RetrieveByVerb(rs).ConfigureAwait(false);
        }

        /// <summary>
        /// This will locate response file based on verb and the current storage.
        /// It will load the ResponseModel based on it and return it.
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        protected virtual Task<ResponseModel> RetrieveByVerb(RequestModel rs)
        {
            // Retrieve the response data from the storage manager based on the HTTP method
            return _currentStorage.RetrieveResponseByVerb(rs.Verb);
        }

        /// <summary>
        /// This is a utility to retrieve response based on file. It combines filename and folder name derived from the url
        /// and retreives the file. This can be used by overridden server simulation method to customize response based on the request.
        /// e.g. if certain value are invalid or they take different actions.
        /// </summary>
        /// <param name="filename">name of the file</param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual Task<ResponseModel> RetrieveByFile(string filename, string url= null)
        {
            // Retrieve the response data from the storage manager based on the file name
            return _currentStorage.RetrieveResponseByFileName(filename, url);
        }

        // sets status code of the response.
        private void SetStatusCode(HttpListenerResponse response, HttpStatusCode statusCode)
        {
            if (!Enum.IsDefined(typeof(HttpStatusCode), statusCode))
            {
                throw new InvalidOperationException($"Invalid StatusCode '{statusCode}' in response data");
            }

            response.StatusCode = (int)statusCode;
        }
        
        // optionally writes headers to response
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

        // writes body of the response based on the content type
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

        // save the request with response as null and return its index
        private int SaveRequest(RequestModel request)
        {
            lock (_lockObj)
            {
                _listReqResp.Add(new Tuple<RequestModel, ResponseModel>(request, null));
                int ind = _listReqResp.Count - 1;

                return ind;
            }
        }

        // Add one at a time. Lock is not efficient. Ideally queuing should be used for performance.
        // But this tool is not for measuring performance.
        private void AddReqResp(int reqIndex, ResponseModel response)
        {
            lock(_lockObj)
            {
                _listReqResp[reqIndex] = new Tuple<RequestModel, ResponseModel>(_listReqResp[reqIndex].Item1, response);
            }
        }
    }
}
